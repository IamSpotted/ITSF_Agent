using System.Text.Json;

namespace DeviceAgent.Services;

public interface ILocalStateService
{
    Task<DateTime?> GetLastCheckInAsync();
    Task SetLastCheckInAsync(DateTime checkInTime);
    Task<bool> HasCheckedInBeforeAsync();
    Task ClearStateAsync();
}

public class LocalStateService : ILocalStateService
{
    private readonly string _stateFilePath;
    private readonly ILogger<LocalStateService> _logger;

    public LocalStateService(ILogger<LocalStateService> logger)
    {
        _logger = logger;
        
        // Store state file in a persistent location
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var agentFolder = Path.Combine(appDataPath, "DeviceAgent");
        Directory.CreateDirectory(agentFolder);
        _stateFilePath = Path.Combine(agentFolder, "agent-state.json");
        
        _logger.LogInformation("State file location: {StateFile}", _stateFilePath);
    }

    public async Task<DateTime?> GetLastCheckInAsync()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                _logger.LogInformation("No state file found, this appears to be first run");
                return null;
            }

            var json = await File.ReadAllTextAsync(_stateFilePath);
            var state = JsonSerializer.Deserialize<AgentState>(json);
            
            _logger.LogInformation("Last check-in was: {LastCheckIn}", state?.LastCheckIn);
            return state?.LastCheckIn;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading state file, treating as first run");
            return null;
        }
    }

    public async Task SetLastCheckInAsync(DateTime checkInTime)
    {
        try
        {
            var state = new AgentState
            {
                LastCheckIn = checkInTime,
                Hostname = Environment.MachineName,
                AgentVersion = "1.0.0"
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(_stateFilePath, json);
            _logger.LogInformation("Updated last check-in time to: {CheckInTime}", checkInTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing state file");
            throw;
        }
    }

    public async Task<bool> HasCheckedInBeforeAsync()
    {
        var lastCheckIn = await GetLastCheckInAsync();
        return lastCheckIn.HasValue;
    }

    public async Task ClearStateAsync()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                File.Delete(_stateFilePath);
                _logger.LogInformation("State file cleared");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing state file");
        }
    }

    private class AgentState
    {
        public DateTime? LastCheckIn { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public string AgentVersion { get; set; } = string.Empty;
    }
}
