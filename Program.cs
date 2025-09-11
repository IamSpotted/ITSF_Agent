using DeviceAgent;
using DeviceAgent.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddScoped<IDeviceInfoService, DeviceInfoService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<ISqlQueryService, SqlQueryService>();
builder.Services.AddScoped<ILocalStateService, LocalStateService>();
builder.Services.AddScoped<IDeviceSyncService, DeviceSyncService>();

// Register the worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
