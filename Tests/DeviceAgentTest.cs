using DeviceAgent.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DeviceAgent.Tests;

public class DeviceAgentTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Device Agent Test - Starting...");

        // Create a simple logger factory
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Create configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        try
        {
            // Test device info collection
            var deviceInfoLogger = loggerFactory.CreateLogger<DeviceInfoService>();
            var deviceInfoService = new DeviceInfoService(deviceInfoLogger);
            
            Console.WriteLine("Collecting device information...");
            var deviceInfo = await deviceInfoService.GetCurrentDeviceInfoAsync();
            
            Console.WriteLine($"Hostname: {deviceInfo.hostname}");
            Console.WriteLine($"Manufacturer: {deviceInfo.manufacturer}");
            Console.WriteLine($"Model: {deviceInfo.model}");
            Console.WriteLine($"CPU: {deviceInfo.cpu_info}");
            Console.WriteLine($"RAM: {deviceInfo.total_ram_gb}GB ({deviceInfo.ram_type})");
            Console.WriteLine($"OS: {deviceInfo.os_name} {deviceInfo.os_version} ({deviceInfo.os_architecture})");
            Console.WriteLine($"Primary IP: {deviceInfo.primary_ip}");
            Console.WriteLine($"Primary MAC: {deviceInfo.primary_mac}");
            Console.WriteLine($"Domain Joined: {deviceInfo.is_domain_joined}");
            Console.WriteLine($"Domain/Workgroup: {deviceInfo.domain_name}");

            Console.WriteLine("\nDevice information collection completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
