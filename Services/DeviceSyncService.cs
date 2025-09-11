using DeviceAgent.Models;

namespace DeviceAgent.Services;

public interface IDeviceSyncService
{
    Task SyncDeviceAsync();
    Task<TimeSpan> GetTimeUntilNextCheckInAsync();
}

public class DeviceSyncService : IDeviceSyncService
{
    private readonly IDeviceInfoService _deviceInfoService;
    private readonly IDatabaseService _databaseService;
    private readonly ILocalStateService _localStateService;
    private readonly ILogger<DeviceSyncService> _logger;
    private readonly TimeSpan _checkInInterval = TimeSpan.FromDays(7); // 7 days between check-ins

    public DeviceSyncService(
        IDeviceInfoService deviceInfoService,
        IDatabaseService databaseService,
        ILocalStateService localStateService,
        ILogger<DeviceSyncService> logger)
    {
        _deviceInfoService = deviceInfoService;
        _databaseService = databaseService;
        _localStateService = localStateService;
        _logger = logger;
    }

    public async Task SyncDeviceAsync()
    {
        try
        {
            _logger.LogInformation("Starting device sync process...");

            // Check if we've checked in before
            var hasCheckedInBefore = await _localStateService.HasCheckedInBeforeAsync();
            var lastCheckIn = await _localStateService.GetLastCheckInAsync();

            if (!hasCheckedInBefore)
            {
                _logger.LogInformation("First run detected - performing initial check-in");
                await PerformFullSyncAsync(isFirstRun: true);
            }
            else
            {
                var timeSinceLastCheckIn = DateTime.UtcNow - lastCheckIn!.Value;
                _logger.LogInformation("Last check-in was {TimeSinceLastCheckIn} ago", timeSinceLastCheckIn);

                if (timeSinceLastCheckIn >= _checkInInterval)
                {
                    _logger.LogInformation("Check-in interval exceeded ({Interval}), performing sync", _checkInInterval);
                    await PerformFullSyncAsync(isFirstRun: false);
                }
                else
                {
                    var timeUntilNext = _checkInInterval - timeSinceLastCheckIn;
                    _logger.LogInformation("Check-in not due yet. Next check-in in: {TimeUntilNext}", timeUntilNext);
                    return; // Exit without doing anything
                }
            }

            _logger.LogInformation("Device sync process completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device sync process");
            throw;
        }
    }

    public async Task<TimeSpan> GetTimeUntilNextCheckInAsync()
    {
        try
        {
            var hasCheckedInBefore = await _localStateService.HasCheckedInBeforeAsync();
            if (!hasCheckedInBefore)
            {
                return TimeSpan.Zero; // Should check in immediately
            }

            var lastCheckIn = await _localStateService.GetLastCheckInAsync();
            if (!lastCheckIn.HasValue)
            {
                return TimeSpan.Zero; // Should check in immediately
            }

            var timeSinceLastCheckIn = DateTime.UtcNow - lastCheckIn.Value;
            if (timeSinceLastCheckIn >= _checkInInterval)
            {
                return TimeSpan.Zero; // Should check in immediately
            }

            var timeUntilNext = _checkInInterval - timeSinceLastCheckIn;
            return timeUntilNext;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating time until next check-in, defaulting to immediate");
            return TimeSpan.Zero;
        }
    }

    private async Task PerformFullSyncAsync(bool isFirstRun)
    {
        try
        {
            // Get current device information
            var currentDeviceInfo = await _deviceInfoService.GetCurrentDeviceInfoAsync();
            _logger.LogInformation("Collected current device info for hostname: {Hostname}", currentDeviceInfo.hostname);

            // Check if device exists in database
            var existingDeviceInfo = await _databaseService.GetDeviceByHostnameAsync(currentDeviceInfo.hostname);

            if (existingDeviceInfo == null)
            {
                // Device doesn't exist, insert new record
                _logger.LogInformation("Device not found in database. Inserting new record for: {Hostname}", currentDeviceInfo.hostname);
                var insertResult = await _databaseService.InsertDeviceAsync(currentDeviceInfo);
                
                if (insertResult)
                {
                    _logger.LogInformation("Successfully inserted new device record for: {Hostname}", currentDeviceInfo.hostname);
                    await _localStateService.SetLastCheckInAsync(DateTime.UtcNow);
                }
                else
                {
                    _logger.LogWarning("Failed to insert new device record for: {Hostname}", currentDeviceInfo.hostname);
                    throw new InvalidOperationException("Failed to insert device record");
                }
            }
            else
            {
                // Device exists, compare and update if needed
                _logger.LogInformation("Device found in database. Comparing current vs stored info for: {Hostname}", currentDeviceInfo.hostname);
                
                var hasChanges = HasSignificantChanges(currentDeviceInfo, existingDeviceInfo);
                
                if (hasChanges)
                {
                    _logger.LogInformation("Changes detected for device: {Hostname}. Updating database...", currentDeviceInfo.hostname);

                    // Preserve the device_id and other database-specific fields
                    currentDeviceInfo.device_id = existingDeviceInfo.device_id;
                    currentDeviceInfo.created_at = existingDeviceInfo.created_at;

                    var updateResult = await _databaseService.UpdateDeviceAsync(currentDeviceInfo);
                    
                    if (updateResult)
                    {
                        _logger.LogInformation("Successfully updated device record for: {Hostname}", currentDeviceInfo.hostname);
                        LogChanges(currentDeviceInfo, existingDeviceInfo);
                        await _localStateService.SetLastCheckInAsync(DateTime.UtcNow);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update device record for: {Hostname}", currentDeviceInfo.hostname);
                        throw new InvalidOperationException("Failed to update device record");
                    }
                }
                else
                {
                    // No changes, just update the last discovered timestamp and local state
                    _logger.LogInformation("No changes detected for device: {Hostname}. Updating last discovered timestamp only.", currentDeviceInfo.hostname);
                    
                    var timestampResult = await _databaseService.UpdateLastDiscoveredAsync(currentDeviceInfo.hostname);
                    
                    if (timestampResult)
                    {
                        _logger.LogInformation("Successfully updated last discovered timestamp for: {Hostname}", currentDeviceInfo.hostname);
                        await _localStateService.SetLastCheckInAsync(DateTime.UtcNow);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update last discovered timestamp for: {Hostname}", currentDeviceInfo.hostname);
                        throw new InvalidOperationException("Failed to update last discovered timestamp");
                    }
                }
            }

            if (isFirstRun)
            {
                _logger.LogInformation("Initial check-in completed successfully. Next check-in scheduled in {Interval}", _checkInInterval);
            }
            else
            {
                _logger.LogInformation("Scheduled check-in completed successfully. Next check-in scheduled in {Interval}", _checkInInterval);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full sync operation");
            throw;
        }
    }

    private bool HasSignificantChanges(DeviceInfo current, DeviceInfo existing)
    {
        // Compare significant fields that would indicate hardware or system changes
        var changes = new List<string>();

        if (!string.Equals(current.serial_number, existing.serial_number, StringComparison.OrdinalIgnoreCase))
            changes.Add($"SerialNumber: '{existing.serial_number}' -> '{current.serial_number}'");

        if (!string.Equals(current.primary_ip, existing.primary_ip, StringComparison.OrdinalIgnoreCase))
            changes.Add($"PrimaryIp: '{existing.primary_ip}' -> '{current.primary_ip}'");

        if (!string.Equals(current.primary_mac, existing.primary_mac, StringComparison.OrdinalIgnoreCase))
            changes.Add($"PrimaryMac: '{existing.primary_mac}' -> '{current.primary_mac}'");

        if (!string.Equals(current.domain_name, existing.domain_name, StringComparison.OrdinalIgnoreCase))
            changes.Add($"DomainName: '{existing.domain_name}' -> '{current.domain_name}'");

        if (current.is_domain_joined != existing.is_domain_joined)
            changes.Add($"IsDomainJoined: '{existing.is_domain_joined}' -> '{current.is_domain_joined}'");

        if (!string.Equals(current.manufacturer, existing.manufacturer, StringComparison.OrdinalIgnoreCase))
            changes.Add($"Manufacturer: '{existing.manufacturer}' -> '{current.manufacturer}'");

        if (!string.Equals(current.model, existing.model, StringComparison.OrdinalIgnoreCase))
            changes.Add($"Model: '{existing.model}' -> '{current.model}'");

        if (!string.Equals(current.cpu_info, existing.cpu_info, StringComparison.OrdinalIgnoreCase))
            changes.Add($"CpuInfo: '{existing.cpu_info}' -> '{current.cpu_info}'");

        if (Math.Abs((current.total_ram_gb ?? 0) - (existing.total_ram_gb ?? 0)) > 1) // Allow for small differences (int comparison)
            changes.Add($"TotalRamGb: '{existing.total_ram_gb}' -> '{current.total_ram_gb}'");

        if (!string.Equals(current.ram_type, existing.ram_type, StringComparison.OrdinalIgnoreCase))
            changes.Add($"RamType: '{existing.ram_type}' -> '{current.ram_type}'");

        if (!string.Equals(current.storage_info, existing.storage_info, StringComparison.OrdinalIgnoreCase))
            changes.Add($"StorageInfo: '{existing.storage_info}' -> '{current.storage_info}'");

        if (!string.Equals(current.bios_version, existing.bios_version, StringComparison.OrdinalIgnoreCase))
            changes.Add($"BiosVersion: '{existing.bios_version}' -> '{current.bios_version}'");

        if (!string.Equals(current.os_name, existing.os_name, StringComparison.OrdinalIgnoreCase))
            changes.Add($"OsName: '{existing.os_name}' -> '{current.os_name}'");

        if (!string.Equals(current.os_version, existing.os_version, StringComparison.OrdinalIgnoreCase))
            changes.Add($"OSVersion: '{existing.os_version}' -> '{current.os_version}'");

        if (!string.Equals(current.os_architecture, existing.os_architecture, StringComparison.OrdinalIgnoreCase))
            changes.Add($"OsArchitecture: '{existing.os_architecture}' -> '{current.os_architecture}'");

        // Store changes for logging
        if (changes.Any())
        {
            _logger.LogInformation("Changes detected: {Changes}", string.Join(", ", changes));
        }

        return changes.Any();
    }

    private void LogChanges(DeviceInfo current, DeviceInfo existing)
    {
        _logger.LogInformation("Device update summary for {Hostname}:", current.hostname);
        _logger.LogInformation("  - Last discovered: {LastDiscovered}", current.last_discovered);
        _logger.LogInformation("  - Updated timestamp: {UpdatedAt}", current.updated_at);

        // Log a few key fields that commonly change
        if (!string.Equals(current.primary_ip, existing.primary_ip))
            _logger.LogInformation("  - IP Address changed: {OldIp} -> {NewIp}", existing.primary_ip, current.primary_ip);

        if (!string.Equals(current.os_version, existing.os_version))
            _logger.LogInformation("  - OS Version changed: {OldVersion} -> {NewVersion}", existing.os_version, current.os_version);

        if (Math.Abs((current.total_ram_gb ?? 0) - (existing.total_ram_gb ?? 0)) > 1)
            _logger.LogInformation("  - RAM changed: {OldRam}GB -> {NewRam}GB", existing.total_ram_gb, current.total_ram_gb);
    }
}
