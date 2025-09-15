using DeviceAgent.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace DeviceAgent.Services;

public interface IConfigurationService
{
    AppConfiguration GetConfiguration();
    Task SaveConfigurationAsync(AppConfiguration config);
    event EventHandler<AppConfiguration>? ConfigurationChanged;
}

public class ConfigurationService : IConfigurationService
{
    private readonly string _configFilePath;
    private AppConfiguration _currentConfig;
    
    public event EventHandler<AppConfiguration>? ConfigurationChanged;

    public ConfigurationService()
    {
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app-config.json");
        _currentConfig = LoadConfiguration();
    }

    public AppConfiguration GetConfiguration()
    {
        return _currentConfig;
    }

    public async Task SaveConfigurationAsync(AppConfiguration config)
    {
        _currentConfig = config;
        
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(_configFilePath, json);
        ConfigurationChanged?.Invoke(this, config);
    }

    private AppConfiguration LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<AppConfiguration>(json) ?? new AppConfiguration();
            }
        }
        catch (Exception)
        {
            // If loading fails, return default configuration
        }
        
        return new AppConfiguration();
    }
}
