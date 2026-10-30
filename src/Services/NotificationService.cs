using HocoKeepAlive.Diagnostics;

namespace HocoKeepAlive.Services;

public static class NotificationService
{
    private static NotifyIcon? _notifyIcon;
    private static bool _lowBatteryNotified;

    public static void Initialize(NotifyIcon icon)
    {
        _notifyIcon = icon;
    }

    public static void CheckBatteryNotification(int batteryLevel, string deviceName, bool enabled)
    {
        if (!enabled || _notifyIcon == null) return;

        if (batteryLevel <= 20 && !_lowBatteryNotified)
        {
            _lowBatteryNotified = true;
            ShowNotification(
                "Low Speaker Battery Warning",
                $"{deviceName} battery is at {batteryLevel}%. Please connect your charger.",
                ToolTipIcon.Warning
            );
            LogService.Log(LogSeverity.Warning, "Notification", $"Low battery notification sent: {batteryLevel}%");
        }
        else if (batteryLevel > 25)
        {
            _lowBatteryNotified = false;
        }
    }

    public static void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        try
        {
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, icon);
            }
        }
        catch (Exception ex)
        {
            LogService.Log(LogSeverity.Warning, "Notification", "Failed to display notification: " + ex.Message);
        }
    }
}
