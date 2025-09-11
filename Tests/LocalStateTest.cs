using DeviceAgent.Services;
using Microsoft.Extensions.Logging;

namespace DeviceAgent.Tests;

class LocalStateTest
{
    static async Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var logger = loggerFactory.CreateLogger<LocalStateService>();
        var stateService = new LocalStateService(logger);

        Console.WriteLine("=== Local State Service Test ===\n");

        // Check if we've checked in before
        var hasCheckedIn = await stateService.HasCheckedInBeforeAsync();
        Console.WriteLine($"Has checked in before: {hasCheckedIn}");

        if (hasCheckedIn)
        {
            var lastCheckIn = await stateService.GetLastCheckInAsync();
            Console.WriteLine($"Last check-in: {lastCheckIn}");
            
            if (lastCheckIn.HasValue)
            {
                var timeSince = DateTime.UtcNow - lastCheckIn.Value;
                Console.WriteLine($"Time since last check-in: {timeSince}");
                Console.WriteLine($"Days since last check-in: {timeSince.TotalDays:F2}");
            }
        }
        else
        {
            Console.WriteLine("This appears to be the first run.");
        }

        // Simulate a check-in
        Console.WriteLine("\nSimulating check-in...");
        await stateService.SetLastCheckInAsync(DateTime.UtcNow);
        Console.WriteLine("Check-in completed!");

        // Verify the check-in was recorded
        var newLastCheckIn = await stateService.GetLastCheckInAsync();
        Console.WriteLine($"New last check-in time: {newLastCheckIn}");

        Console.WriteLine("\n=== Test Complete ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
