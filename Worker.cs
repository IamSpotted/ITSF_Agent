using DeviceAgent.Services;

namespace DeviceAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDeviceSyncService _deviceSyncService;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour to see if it's time
    private readonly string _triggerFilePath;

    public Worker(ILogger<Worker> logger, IDeviceSyncService deviceSyncService, IConfiguration configuration)
    {
        _logger = logger;
        _deviceSyncService = deviceSyncService;
        _configuration = configuration;
        
        // Set up trigger file path (in the same directory as the executable)
        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var exeDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory;
        _triggerFilePath = Path.Combine(exeDirectory, "force_sync.trigger");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device Agent Worker starting...");

        try
        {
            // Run sync immediately on startup (will check if it's needed)
            _logger.LogInformation("Performing startup device sync check...");
            await _deviceSyncService.SyncDeviceAsync();
            _logger.LogInformation("Startup device sync check completed.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check for force sync trigger file
                    if (CheckForForceSyncTrigger())
                    {
                        _logger.LogInformation("Force sync trigger detected, performing immediate sync...");
                        await _deviceSyncService.SyncDeviceAsync();
                        continue; // Go to next iteration immediately
                    }

                    // Calculate how long to wait until next check-in
                    var timeUntilNext = await _deviceSyncService.GetTimeUntilNextCheckInAsync();
                    
                    if (timeUntilNext <= TimeSpan.Zero)
                    {
                        _logger.LogInformation("Check-in is due, performing sync...");
                        await _deviceSyncService.SyncDeviceAsync();
                        
                        // Recalculate after sync
                        timeUntilNext = await _deviceSyncService.GetTimeUntilNextCheckInAsync();
                    }

                    // Wait either until the next check-in is due, or for our check interval (whichever is shorter)
                    var waitTime = timeUntilNext > _checkInterval ? _checkInterval : timeUntilNext;
                    
                    if (waitTime > TimeSpan.Zero)
                    {
                        var nextCheckTime = DateTime.UtcNow.Add(waitTime);
                        _logger.LogInformation("Next check scheduled for: {NextCheck} UTC (in {WaitTime})", 
                            nextCheckTime, waitTime);
                        
                        await Task.Delay(waitTime, stoppingToken);
                    }
                    else
                    {
                        // Small delay to prevent tight loop
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    _logger.LogInformation("Device sync cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduled device sync. Will retry in {RetryInterval}.", _checkInterval);
                    
                    // Wait for the check interval before retrying
                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in Device Agent Worker. Service will stop.");
            throw;
        }

        _logger.LogInformation("Device Agent Worker stopping...");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Device Agent Worker stop requested.");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Device Agent Worker stopped.");
    }

    private bool CheckForForceSyncTrigger()
    {
        try
        {
            if (File.Exists(_triggerFilePath))
            {
                _logger.LogInformation("Force sync trigger file found at: {TriggerPath}", _triggerFilePath);
                
                // Delete the trigger file
                File.Delete(_triggerFilePath);
                _logger.LogInformation("Force sync trigger file removed.");
                
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for force sync trigger file: {TriggerPath}", _triggerFilePath);
        }
        
        return false;
    }
}
