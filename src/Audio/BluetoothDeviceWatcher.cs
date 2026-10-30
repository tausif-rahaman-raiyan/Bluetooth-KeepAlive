using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using HocoKeepAlive.Diagnostics;

namespace HocoKeepAlive.Audio;

public sealed class BluetoothDeviceWatcher : IDisposable
{
    private DeviceWatcher? _watcher;
    private bool _isWatching;

    public event Action<string, string>? DeviceConnected;
    public event Action<string, string>? DeviceDisconnected;

    public bool IsWatching => _isWatching;

    public void StartWatcher()
    {
        if (_isWatching) return;

        try
        {
            string selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            _watcher = DeviceInformation.CreateWatcher(
                selector,
                new[] { "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" }
            );

            _watcher.Added += OnDeviceAdded;
            _watcher.Updated += OnDeviceUpdated;
            _watcher.Removed += OnDeviceRemoved;

            _watcher.Start();
            _isWatching = true;

            LogService.Log(LogSeverity.Info, "DeviceWatcher", "Native WinRT Bluetooth DeviceWatcher active.");
        }
        catch (Exception ex)
        {
            LogService.Log(LogSeverity.Warning, "DeviceWatcher", "Failed to start WinRT DeviceWatcher: " + ex.Message);
        }
    }

    private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation info)
    {
        bool isConnected = false;
        if (info.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var connectedVal) && connectedVal is bool conn)
        {
            isConnected = conn;
        }

        if (isConnected || info.Name.Contains("HOCO", StringComparison.OrdinalIgnoreCase) || info.Name.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
        {
            LogService.Log(LogSeverity.Info, "DeviceWatcher", $"Bluetooth device detected: {info.Name} (ID: {info.Id})");
            DeviceConnected?.Invoke(info.Name, info.Id);
        }
    }

    private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        if (update.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var connVal) && connVal is bool isConnected)
        {
            if (isConnected)
            {
                LogService.Log(LogSeverity.Info, "DeviceWatcher", $"Bluetooth device connected event: {update.Id}");
                DeviceConnected?.Invoke("Bluetooth Device", update.Id);
            }
            else
            {
                LogService.Log(LogSeverity.Warning, "DeviceWatcher", $"Bluetooth device disconnected event: {update.Id}");
                DeviceDisconnected?.Invoke("Bluetooth Device", update.Id);
            }
        }
    }

    private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        LogService.Log(LogSeverity.Warning, "DeviceWatcher", $"Bluetooth device removed: {update.Id}");
        DeviceDisconnected?.Invoke("Bluetooth Device", update.Id);
    }

    public void StopWatcher()
    {
        if (!_isWatching || _watcher == null) return;

        try
        {
            _watcher.Stop();
            _isWatching = false;
            LogService.Log(LogSeverity.Info, "DeviceWatcher", "WinRT DeviceWatcher stopped.");
        }
        catch { }
    }

    public void Dispose()
    {
        StopWatcher();
        _watcher = null;
    }
}
