namespace DeviceAgent.Models;

public class AppConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CheckInIntervalDays { get; set; } = 7;
    public bool ShowGuiAtStartup { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public string TimeZoneId { get; set; } = "Eastern Standard Time";
}
