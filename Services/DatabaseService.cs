using Microsoft.Data.SqlClient;
using DeviceAgent.Models;
using System.Data;

namespace DeviceAgent.Services;

public interface IDatabaseService
{
    Task<DeviceInfo?> GetDeviceByHostnameAsync(string hostname);
    Task<bool> InsertDeviceAsync(DeviceInfo deviceInfo);
    Task<bool> UpdateDeviceAsync(DeviceInfo deviceInfo);
    Task<bool> UpdateLastDiscoveredAsync(string hostname);
    Task<bool> TestConnectionAsync();
}

public class DatabaseService : IDatabaseService
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<DatabaseService> _logger;
    private readonly ISqlQueryService _sqlQueryService;

    public DatabaseService(IConfigurationService configService, ILogger<DatabaseService> logger, ISqlQueryService sqlQueryService)
    {
        _configService = configService;
        _logger = logger;
        _sqlQueryService = sqlQueryService;
    }

    private string GetConnectionString()
    {
        var config = _configService.GetConfiguration();
        if (string.IsNullOrEmpty(config.ConnectionString))
        {
            _logger.LogError("Database connection string is not configured");
            throw new InvalidOperationException("Database connection string is not configured. Please configure it through the GUI or configuration file.");
        }
        
        _logger.LogDebug("Using connection string: {ConnectionString}", 
            config.ConnectionString.Replace("Password=", "Password=***").Replace("pwd=", "pwd=***"));
        
        return config.ConnectionString;
    }

    public async Task<DeviceInfo?> GetDeviceByHostnameAsync(string hostname)
    {
        try
        {
            _logger.LogDebug("Attempting to retrieve device by hostname: {Hostname}", hostname);
            
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            _logger.LogDebug("Database connection opened successfully");

            var query = _sqlQueryService.GetDeviceByHostnameQuery();
            _logger.LogDebug("Executing query: {Query}", query);

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@hostname", hostname);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                _logger.LogDebug("Device found in database for hostname: {Hostname}", hostname);
                return MapReaderToDeviceInfo(reader);
            }
            
            _logger.LogDebug("No device found in database for hostname: {Hostname}", hostname);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device by hostname: {Hostname}", hostname);
            throw;
        }
    }

    public async Task<bool> UpdateDeviceAsync(DeviceInfo deviceInfo)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            var query = _sqlQueryService.GetUpdateDeviceQuery();

            using var command = new SqlCommand(query, connection);
            AddParametersToCommand(command, deviceInfo);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device: {Hostname}", deviceInfo.hostname);
            throw;
        }
    }

    public async Task<bool> InsertDeviceAsync(DeviceInfo deviceInfo)
    {
        try
        {
            _logger.LogInformation("Attempting to insert new device: {Hostname}", deviceInfo.hostname);
            
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            _logger.LogDebug("Database connection opened for device insert");

            var query = _sqlQueryService.GetInsertDeviceQuery();
            _logger.LogDebug("Executing insert query for device: {Hostname}", deviceInfo.hostname);

            using var command = new SqlCommand(query, connection);
            deviceInfo.created_at = DateTime.UtcNow;
            AddParametersToCommand(command, deviceInfo);
            command.Parameters.AddWithValue("@CreatedAt", deviceInfo.created_at);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Successfully inserted device: {Hostname} ({RowsAffected} rows affected)", 
                    deviceInfo.hostname, rowsAffected);
            }
            else
            {
                _logger.LogWarning("Device insert returned 0 rows affected for hostname: {Hostname}", 
                    deviceInfo.hostname);
            }
            
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting device: {Hostname}", deviceInfo.hostname);
            throw;
        }
    }

    public async Task<bool> UpdateLastDiscoveredAsync(string hostname)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            var query = _sqlQueryService.GetUpdateLastDiscoveredQuery();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Hostname", hostname);
            command.Parameters.AddWithValue("@LastDiscovered", DateTime.UtcNow);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last discovered for device: {Hostname}", hostname);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing database connection...");
            
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            
            // Simple test query
            using var command = new SqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            
            _logger.LogInformation("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    private void AddParametersToCommand(SqlCommand command, DeviceInfo deviceInfo)
    {
        command.Parameters.AddWithValue("@Hostname", deviceInfo.hostname);
        command.Parameters.AddWithValue("@DeviceStatus", deviceInfo.device_status ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DeviceType", deviceInfo.device_type ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SerialNumber", deviceInfo.serial_number ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@AssetTag", deviceInfo.asset_tag ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@EquipmentGroup", deviceInfo.equipment_group ?? (object)DBNull.Value);
        
        // Domain/Workgroup Information
        command.Parameters.AddWithValue("@DomainName", deviceInfo.domain_name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@IsDomainJoined", deviceInfo.is_domain_joined ?? (object)DBNull.Value);
        
        // Hardware Information
        command.Parameters.AddWithValue("@Manufacturer", deviceInfo.manufacturer ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Model", deviceInfo.model ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CpuInfo", deviceInfo.cpu_info ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@BiosVersion", deviceInfo.bios_version ?? (object)DBNull.Value);
        
        // Memory Information
        command.Parameters.AddWithValue("@TotalRamGb", deviceInfo.total_ram_gb ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RamType", deviceInfo.ram_type ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RamSpeed", deviceInfo.ram_speed ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RamManufacturer", deviceInfo.ram_manufacturer ?? (object)DBNull.Value);
        
        // Operating System Information
        command.Parameters.AddWithValue("@OsName", deviceInfo.os_name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OSVersion", deviceInfo.os_version ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OsArchitecture", deviceInfo.os_architecture ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OsInstallDate", deviceInfo.os_install_date ?? (object)DBNull.Value);
        
        // Primary Storage Information
        command.Parameters.AddWithValue("@StorageInfo", deviceInfo.storage_info ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StorageType", deviceInfo.storage_type ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StorageModel", deviceInfo.storage_model ?? (object)DBNull.Value);
        
        // Additional Storage Drives
        command.Parameters.AddWithValue("@Drive2Name", deviceInfo.drive2_name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive2Capacity", deviceInfo.drive2_capacity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive2Type", deviceInfo.drive2_type ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive2Model", deviceInfo.drive2_model ?? (object)DBNull.Value);
        
        command.Parameters.AddWithValue("@Drive3Name", deviceInfo.drive3_name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive3Capacity", deviceInfo.drive3_capacity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive3Type", deviceInfo.drive3_type ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive3Model", deviceInfo.drive3_model ?? (object)DBNull.Value);
        
        command.Parameters.AddWithValue("@Drive4Name", deviceInfo.drive4_name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive4Capacity", deviceInfo.drive4_capacity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive4Type", deviceInfo.drive4_type ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Drive4Model", deviceInfo.drive4_model ?? (object)DBNull.Value);
        
        // Primary Network Interface
        command.Parameters.AddWithValue("@PrimaryIp", deviceInfo.primary_ip ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PrimaryMac", deviceInfo.primary_mac ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PrimarySubnet", deviceInfo.primary_subnet ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PrimaryDns", deviceInfo.primary_dns ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SecondaryDns", deviceInfo.secondary_dns ?? (object)DBNull.Value);
        
        // Additional Network Interfaces
        command.Parameters.AddWithValue("@Nic2Name", deviceInfo.nic2_name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic2Ip", deviceInfo.nic2_ip ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic2Mac", deviceInfo.nic2_mac ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic2Subnet", deviceInfo.nic2_subnet ?? (object)DBNull.Value);
        
        command.Parameters.AddWithValue("@Nic3Name", deviceInfo.nic3_name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic3Ip", deviceInfo.nic3_ip ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic3Mac", deviceInfo.nic3_mac ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic3Subnet", deviceInfo.nic3_subnet ?? (object)DBNull.Value);
        
        command.Parameters.AddWithValue("@Nic4Name", deviceInfo.nic4_name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic4Ip", deviceInfo.nic4_ip ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic4Mac", deviceInfo.nic4_mac ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nic4Subnet", deviceInfo.nic4_subnet ?? (object)DBNull.Value);
        
        // Web Interface
        command.Parameters.AddWithValue("@WebInterfaceUrl", deviceInfo.web_interface_url ?? (object)DBNull.Value);
        
        // Device Status and Location
        command.Parameters.AddWithValue("@Area", deviceInfo.area ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Zone", deviceInfo.zone ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Line", deviceInfo.line ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Pitch", deviceInfo.pitch ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Floor", deviceInfo.floor ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Pillar", deviceInfo.pillar ?? (object)DBNull.Value);
        
        // Additional Information
        command.Parameters.AddWithValue("@AdditionalNotes", deviceInfo.additional_notes ?? (object)DBNull.Value);
        
        // Asset Management Dates
        command.Parameters.AddWithValue("@PurchaseDate", deviceInfo.purchase_date ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ServiceDate", deviceInfo.service_date ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@WarrantyDate", deviceInfo.warranty_date ?? (object)DBNull.Value);
        
        // System Fields
        command.Parameters.AddWithValue("@UpdatedAt", deviceInfo.updated_at ?? DateTime.UtcNow);
        command.Parameters.AddWithValue("@LastDiscovered", deviceInfo.last_discovered ?? DateTime.UtcNow);
        command.Parameters.AddWithValue("@DiscoveryMethod", deviceInfo.discovery_method ?? (object)DBNull.Value);
    }

    private DeviceInfo MapReaderToDeviceInfo(SqlDataReader reader)
    {
        return new DeviceInfo
        {
            device_id = reader.GetOrdinal("device_id") >= 0 ? reader.GetInt32("device_id") : 0,
            hostname = reader["hostname"]?.ToString() ?? string.Empty,
            device_status = reader["device_status"]?.ToString() ?? string.Empty,
            device_type = reader["device_type"]?.ToString() ?? "Other",
            serial_number = reader["serial_number"]?.ToString(),
            asset_tag = reader["asset_tag"]?.ToString(),
            equipment_group = reader["equipment_group"]?.ToString(),
            
            // Domain/Workgroup Information
            domain_name = reader["domain_name"]?.ToString(),
            is_domain_joined = reader["is_domain_joined"] as bool?,
            
            // Hardware Information
            manufacturer = reader["manufacturer"]?.ToString(),
            model = reader["model"]?.ToString(),
            cpu_info = reader["cpu_info"]?.ToString(),
            bios_version = reader["bios_version"]?.ToString(),
            
            // Memory Information
            total_ram_gb = reader["total_ram_gb"] != DBNull.Value ? Convert.ToInt32(reader["total_ram_gb"]) : null,
            ram_type = reader["ram_type"]?.ToString(),
            ram_speed = reader["ram_speed"]?.ToString(),
            ram_manufacturer = reader["ram_manufacturer"]?.ToString(),
            
            // Operating System Information
            os_name = reader["os_name"]?.ToString(),
            os_version = reader["os_version"]?.ToString(),
            os_architecture = reader["os_architecture"]?.ToString(),
            os_install_date = reader["os_install_date"] as DateTime?,
            
            // Primary Storage Information
            storage_info = reader["storage_info"]?.ToString(),
            storage_type = reader["storage_type"]?.ToString(),
            storage_model = reader["storage_model"]?.ToString(),
            
            // Additional Storage Drives
            drive2_name = reader["drive2_name"]?.ToString(),
            drive2_capacity = reader["drive2_capacity"]?.ToString(),
            drive2_type = reader["drive2_type"]?.ToString(),
            drive2_model = reader["drive2_model"]?.ToString(),
            
            drive3_name = reader["drive3_name"]?.ToString(),
            drive3_capacity = reader["drive3_capacity"]?.ToString(),
            drive3_type = reader["drive3_type"]?.ToString(),
            drive3_model = reader["drive3_model"]?.ToString(),
            
            drive4_name = reader["drive4_name"]?.ToString(),
            drive4_capacity = reader["drive4_capacity"]?.ToString(),
            drive4_type = reader["drive4_type"]?.ToString(),
            drive4_model = reader["drive4_model"]?.ToString(),
            
            // Primary Network Interface
            primary_ip = reader["primary_ip"]?.ToString(),
            primary_mac = reader["primary_mac"]?.ToString(),
            primary_subnet = reader["primary_subnet"]?.ToString(),
            primary_dns = reader["primary_dns"]?.ToString(),
            secondary_dns = reader["secondary_dns"]?.ToString(),
            
            // Additional Network Interfaces
            nic2_name = reader["nic2_name"]?.ToString(),
            nic2_ip = reader["nic2_ip"]?.ToString(),
            nic2_mac = reader["nic2_mac"]?.ToString(),
            nic2_subnet = reader["nic2_subnet"]?.ToString(),
            
            nic3_name = reader["nic3_name"]?.ToString(),
            nic3_ip = reader["nic3_ip"]?.ToString(),
            nic3_mac = reader["nic3_mac"]?.ToString(),
            nic3_subnet = reader["nic3_subnet"]?.ToString(),
            
            nic4_name = reader["nic4_name"]?.ToString(),
            nic4_ip = reader["nic4_ip"]?.ToString(),
            nic4_mac = reader["nic4_mac"]?.ToString(),
            nic4_subnet = reader["nic4_subnet"]?.ToString(),
            
            // Web Interface
            web_interface_url = reader["web_interface_url"]?.ToString(),
            
            // Device Status and Location
            area = reader["area"]?.ToString() ?? string.Empty,
            zone = reader["zone"]?.ToString() ?? string.Empty,
            line = reader["line"]?.ToString() ?? string.Empty,
            pitch = reader["pitch"]?.ToString() ?? string.Empty,
            floor = reader["floor"]?.ToString() ?? string.Empty,
            pillar = reader["pillar"]?.ToString() ?? string.Empty,
            
            // Additional Information
            additional_notes = reader["additional_notes"]?.ToString(),
            
            // Asset Management Dates
            purchase_date = reader["purchase_date"] as DateTime?,
            service_date = reader["service_date"] as DateTime?,
            warranty_date = reader["warranty_date"] as DateTime?,
            
            // System Fields
            created_at = reader["created_at"] as DateTime?,
            updated_at = reader["updated_at"] as DateTime?,
            last_discovered = reader["last_discovered"] as DateTime?,
            discovery_method = reader["discovery_method"]?.ToString()
        };
    }
}
