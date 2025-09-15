using DeviceAgent.Models;

namespace DeviceAgent.Services;

public interface ITimeZoneService
{
    DateTime GetCurrentTime();
    DateTime ConvertUtcToLocal(DateTime utcTime);
    DateTime ConvertLocalToUtc(DateTime localTime);
    string GetTimeZoneId();
}

public class TimeZoneService : ITimeZoneService
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<TimeZoneService> _logger;
    private TimeZoneInfo _timeZone;

    public TimeZoneService(IConfigurationService configService, ILogger<TimeZoneService> logger)
    {
        _configService = configService;
        _logger = logger;
        _timeZone = LoadTimeZone();
    }

    public DateTime GetCurrentTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
    }

    public DateTime ConvertUtcToLocal(DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _timeZone);
    }

    public DateTime ConvertLocalToUtc(DateTime localTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localTime, _timeZone);
    }

    public string GetTimeZoneId()
    {
        return _timeZone.Id;
    }

    private TimeZoneInfo LoadTimeZone()
    {
        try
        {
            var config = _configService.GetConfiguration();
            var timeZoneId = config.TimeZoneId;

            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                _logger.LogWarning("No timezone configured, using system local timezone");
                return TimeZoneInfo.Local;
            }

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            _logger.LogInformation("Using timezone: {TimeZoneId} ({DisplayName})", timeZone.Id, timeZone.DisplayName);
            return timeZone;
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogError(ex, "Configured timezone '{TimeZoneId}' not found, falling back to local timezone", _configService.GetConfiguration().TimeZoneId);
            return TimeZoneInfo.Local;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading timezone configuration, using local timezone");
            return TimeZoneInfo.Local;
        }
    }
}