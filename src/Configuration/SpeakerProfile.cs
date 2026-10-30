namespace HocoKeepAlive.Configuration;

public sealed class SpeakerProfile
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool AutoStartOnConnect { get; set; } = true;
    public bool EnableAntiSleepDither { get; set; } = false;
    public DateTime LastConnectedUtc { get; set; } = DateTime.UtcNow;
}
