using DeviceAgent.Services;

namespace DeviceAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDeviceSyncService _deviceSyncService;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour to see if it's time

    public Worker(ILogger<Worker> logger, IDeviceSyncService deviceSyncService)
    {
        _logger = logger;
        _deviceSyncService = deviceSyncService;
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
}
