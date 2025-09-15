using DeviceAgent;
using DeviceAgent.Services;
#if WINDOWS
using DeviceAgent.GUI;
using Microsoft.Extensions.Hosting.WindowsServices;
#endif
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

#if WINDOWS
// Configure Windows service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DeviceAgent";
});
#endif

// Register services
builder.Services.AddScoped<IDeviceInfoService, DeviceInfoService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<ISqlQueryService, SqlQueryService>();
builder.Services.AddScoped<ILocalStateService, LocalStateService>();
builder.Services.AddScoped<IDeviceSyncService, DeviceSyncService>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<IGUILoggerService, GUILoggerService>();
builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

#if WINDOWS
// Additional Windows-specific services can be registered here if needed
#endif

// Register the worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Check if we should show GUI
var configService = host.Services.GetRequiredService<IConfigurationService>();
var config = configService.GetConfiguration();

#if WINDOWS
// If GUI is enabled and running interactively (not as a service), start the Windows Forms application
if (config.ShowGuiAtStartup && Environment.UserInteractive)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting GUI mode...");
    
    System.Windows.Forms.Application.EnableVisualStyles();
    System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
    
    // Start the host in the background
    var hostTask = Task.Run(() => host.RunAsync());
    
    // Create and show the main form
    var deviceInfoService = host.Services.GetRequiredService<IDeviceInfoService>();
    var deviceSyncService = host.Services.GetRequiredService<IDeviceSyncService>();
    var mainFormLogger = host.Services.GetRequiredService<ILogger<MainForm>>();
    var guiLoggerService = host.Services.GetRequiredService<IGUILoggerService>();
    
    var mainForm = new MainForm(deviceInfoService, deviceSyncService, configService, mainFormLogger);
    
    // Connect the GUI logger to the main form
    guiLoggerService.SetMainForm(mainForm);
    
    try
    {
        System.Windows.Forms.Application.Run(mainForm);
    }
    finally
    {
        logger.LogInformation("GUI closed, shutting down...");
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        await hostTask;
    }
}
else
#endif
{
    // Run in headless mode
    await host.RunAsync();
}
