using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using HocoKeepAlive.Audio;
using HocoKeepAlive.Configuration;
using HocoKeepAlive.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace HocoKeepAlive.UI;

/// <summary>
/// Main window that hosts the exact same React UI used in the website / AI Studio preview
/// (built from /web into wwwroot) inside a WebView2 control, and bridges it to the native
/// audio keep-alive engine via JSON web messages.
/// </summary>
public sealed class WebMainForm : Form
{
    private const string VirtualHost = "keepalive.app";
    private static readonly string WwwRoot = Path.Combine(
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, "wwwroot");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AudioKeepAliveService _audio;
    private readonly AppSettings _settings;
    private readonly Action _onExit;
    private readonly WebView2 _web;
    private readonly System.Windows.Forms.Timer _timer;

    private bool _pageReady;
    private string? _pendingPage;
    private int _tick;
    private volatile bool _telemetryBusy;
    private BluetoothDeviceTelemetry? _telemetry;
    private List<AudioDeviceInfo> _devices = new();

    public WebMainForm(AudioKeepAliveService audio, AppSettings settings, Icon appIcon, Action onExit)
    {
        _audio = audio;
        _settings = settings;
        _onExit = onExit;

        Icon = appIcon;
        Text = "Bluetooth KeepAlive";
        BackColor = Color.FromArgb(11, 12, 16);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1180, 760);
        MinimumSize = new Size(940, 620);

        _web = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.FromArgb(11, 12, 16),
            AllowExternalDrop = false
        };
        Controls.Add(_web);

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => OnTick();

        _audio.StateChanged += OnAudioStateChanged;
        FormClosed += (_, _) =>
        {
            _timer.Stop();
            _audio.StateChanged -= OnAudioStateChanged;
        };
    }

    /// <summary>True when the WebView2 runtime and the built web UI are both available.</summary>
    public static bool IsSupported(out string reason)
    {
        try
        {
            string? version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            if (string.IsNullOrWhiteSpace(version))
            {
                reason = "WebView2 runtime is not installed.";
                return false;
            }

            if (!File.Exists(Path.Combine(WwwRoot, "index.html")))
            {
                reason = $"Web UI bundle not found at {WwwRoot}.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    public void NavigateTo(string page)
    {
        string tab = page.ToLowerInvariant() switch
        {
            "smart controls" or "engine" or "controls" => "controls",
            "audio devices" or "devices" => "devices",
            "settings" => "settings",
            _ => "dashboard"
        };

        if (_pageReady)
        {
            Post(new { type = "navigate", tab });
        }
        else
        {
            _pendingPage = tab;
        }
    }

    // ------------------------------------------------------------------
    // Window / dark title bar
    // ------------------------------------------------------------------

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int dark = 1;
            DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));   // DWMWA_USE_IMMERSIVE_DARK_MODE
            int caption = 0x00100C0B;                                    // BGR of #0B0C10
            DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(int)); // DWMWA_CAPTION_COLOR (Win11)
            int text = 0x00F8F4F3;
            DwmSetWindowAttribute(Handle, 36, ref text, sizeof(int));    // DWMWA_TEXT_COLOR (Win11)
        }
        catch { /* Older Windows builds simply ignore these attributes. */ }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            await InitializeWebAsync();
        }
        catch (Exception ex)
        {
            LogService.Log(LogSeverity.Error, "WebUI", "Failed to initialize WebView2 UI.", ex.ToString());
            MessageBox.Show(this, "The dashboard could not be loaded.\n\n" + ex.Message, "Keep Alive",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task InitializeWebAsync()
    {
        string userData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeepAlive", "WebView2");
        Directory.CreateDirectory(userData);

        var env = await CoreWebView2Environment.CreateAsync(null, userData);
        await _web.EnsureCoreWebView2Async(env);

        var core = _web.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
#if !DEBUG
        core.Settings.AreDevToolsEnabled = false;
#endif

        core.SetVirtualHostNameToFolderMapping(VirtualHost, WwwRoot, CoreWebView2HostResourceAccessKind.Allow);

        core.WebMessageReceived += OnWebMessageReceived;
        core.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            OpenExternal(args.Uri);
        };
        core.NavigationStarting += (_, args) =>
        {
            if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                !uri.Host.Equals(VirtualHost, StringComparison.OrdinalIgnoreCase))
            {
                args.Cancel = true;
                OpenExternal(args.Uri);
            }
        };

        core.Navigate($"https://{VirtualHost}/index.html");
        _timer.Start();
    }

    // ------------------------------------------------------------------
    // Host -> Web
    // ------------------------------------------------------------------

    private void Post(object message)
    {
        if (IsDisposed || _web.CoreWebView2 == null) return;

        try
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Post(message)));
                return;
            }

            _web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonOptions));
        }
        catch (Exception ex)
        {
            LogService.Log(LogSeverity.Warning, "WebUI", "Failed to post message to UI.", ex.Message);
        }
    }

    private void Toast(string message) => Post(new { type = "toast", message });

    private void OnAudioStateChanged(AudioEngineState state, string? message)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke(new Action(PushState));
        }
        catch (InvalidOperationException) { /* shutting down */ }
    }

    private void OnTick()
    {
        _tick++;

        if (_tick % 5 == 1)
        {
            try { _devices = AudioDeviceService.GetPlaybackDevices(); } catch { }
        }

        if (!_telemetryBusy)
        {
            _telemetryBusy = true;
            string name = _settings.PreferredDeviceName;
            _ = Task.Run(() =>
            {
                try { _telemetry = BluetoothManagerService.GetSpeakerTelemetry(name); }
                catch { }
                finally { _telemetryBusy = false; }
            });
        }

        PushState();
    }

    private void PushState()
    {
        if (!_pageReady || IsDisposed) return;

        var state = _audio.State;
        bool running = state is AudioEngineState.Starting or AudioEngineState.Running
            or AudioEngineState.PausedByOtherAudio or AudioEngineState.PausedByCallOrMic
            or AudioEngineState.Recovering;

        var tele = _telemetry;
        var power = SystemInformation.PowerStatus;

        int laptopLevel = 100;
        bool laptopCharging = true;
        bool hasBattery = true;
        try
        {
            float pct = power.BatteryLifePercent;
            if (pct is >= 0f and <= 1f) laptopLevel = (int)Math.Round(pct * 100);
            laptopCharging = power.PowerLineStatus == PowerLineStatus.Online;
            hasBattery = (power.BatteryChargeStatus & BatteryChargeStatus.NoSystemBattery) == 0;
        }
        catch { }

        Post(new
        {
            type = "state",
            version = UpdateService.CurrentVersion,
            engineState = state.ToString(),
            isRunning = running,
            runningSeconds = (int)(_audio.RunningDuration?.TotalSeconds ?? 0),
            lastError = _audio.LastError,
            deviceName = _settings.PreferredDeviceName,
            deviceNumber = _settings.PreferredDeviceNumber,
            devices = _devices.Select(d => new
            {
                number = d.DeviceNumber,
                name = d.Name,
                isDefault = d.IsDefault,
                isBluetooth = d.IsBluetooth
            }),
            speaker = new
            {
                name = tele?.DeviceName ?? _settings.PreferredDeviceName,
                connected = tele?.IsConnected ?? false,
                battery = tele?.BatteryPercentage,
                charging = tele?.IsCharging ?? false,
                status = tele?.StatusDescription ?? "Detecting Bluetooth speaker..."
            },
            laptop = new { level = laptopLevel, charging = laptopCharging, hasBattery },
            settings = new
            {
                alwaysKeepStreamActive = _settings.AlwaysKeepStreamActive,
                autoReconnect = _settings.AutoReconnectOnDeviceAvailable,
                callProtection = _settings.CallProtectionMode,
                autoPauseOnOtherAudio = _settings.AutoPauseOnOtherAudio,
                startOnWindowsStartup = _settings.StartOnWindowsStartup,
                minimizeToTrayOnClose = _settings.MinimizeToTrayOnClose,
                showNotifications = _settings.ShowNotifications
            }
        });
    }

    // ------------------------------------------------------------------
    // Web -> Host
    // ------------------------------------------------------------------

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

            switch (type)
            {
                case "ready":
                    _pageReady = true;
                    if (_devices.Count == 0) _devices = AudioDeviceService.GetPlaybackDevices();
                    PushState();
                    if (_pendingPage != null)
                    {
                        Post(new { type = "navigate", tab = _pendingPage });
                        _pendingPage = null;
                    }
                    break;

                case "toggle":
                    _audio.Toggle();
                    break;

                case "start":
                    _audio.Start();
                    break;

                case "stop":
                    _audio.Stop();
                    break;

                case "selectDevice":
                    SelectDevice(root);
                    break;

                case "refreshDevices":
                    _devices = AudioDeviceService.GetPlaybackDevices();
                    PushState();
                    break;

                case "setSetting":
                    ApplySetting(root);
                    break;

                case "repair":
                    RunRepair();
                    break;

                case "checkUpdate":
                    _ = CheckUpdateAsync();
                    break;

                case "applyUpdate":
                    _ = ApplyUpdateAsync(root.TryGetProperty("url", out var u) ? u.GetString() : null);
                    break;

                case "openUrl":
                    OpenExternal(root.TryGetProperty("url", out var link) ? link.GetString() : null);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogService.Log(LogSeverity.Warning, "WebUI", "Bad message from UI.", ex.Message);
        }
    }

    private void SelectDevice(JsonElement root)
    {
        int number = root.TryGetProperty("number", out var n) && n.TryGetInt32(out int num) ? num : -1;
        string name = root.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(name)) return;

        _settings.PreferredDeviceNumber = number;
        _settings.PreferredDeviceName = name;
        _settings.Save();
        _audio.TargetDeviceNumber = number;
        _audio.PreferredDeviceName = name;

        Toast($"Switched keep-alive output to {name}");
        PushState();
    }

    private void ApplySetting(JsonElement root)
    {
        string key = root.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
        bool value = root.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.True;

        switch (key)
        {
            case "alwaysKeepStreamActive":
                _settings.AlwaysKeepStreamActive = value;
                _audio.AlwaysKeepStreamActive = value;
                break;
            case "autoReconnect":
                _settings.AutoReconnectOnDeviceAvailable = value;
                _audio.AutoReconnect = value;
                break;
            case "callProtection":
                _settings.CallProtectionMode = value;
                _audio.CallProtectionMode = value;
                break;
            case "autoPauseOnOtherAudio":
                _settings.AutoPauseOnOtherAudio = value;
                _audio.AutoPauseOnOtherAudio = value;
                break;
            case "startOnWindowsStartup":
                _settings.StartOnWindowsStartup = value;
                break;
            case "minimizeToTrayOnClose":
                _settings.MinimizeToTrayOnClose = value;
                break;
            case "showNotifications":
                _settings.ShowNotifications = value;
                break;
            default:
                return;
        }

        _settings.Save();
        PushState();
    }

    private void RunRepair()
    {
        string name = _settings.PreferredDeviceName;
        Toast($"Re-pairing {name}...");
        _ = Task.Run(() =>
        {
            bool ok = BluetoothManagerService.TriggerAutoRePairAndConnect(name);
            Toast(ok ? $"Re-pair signal sent to {name}" : $"Could not re-pair {name}");
        });
    }

    private async Task CheckUpdateAsync()
    {
        try
        {
            var info = await UpdateService.CheckForUpdatesAsync();
            Post(new
            {
                type = "update",
                currentVersion = info.CurrentVersion,
                latestVersion = info.LatestVersion,
                isUpdateAvailable = info.IsUpdateAvailable,
                releaseName = info.ReleaseName,
                releaseNotes = info.ReleaseNotes,
                publishedAt = info.PublishedAt,
                setupUrl = info.SetupDownloadUrl,
                htmlUrl = info.HtmlUrl
            });
        }
        catch (Exception ex)
        {
            Toast("Update check failed: " + ex.Message);
        }
    }

    private async Task ApplyUpdateAsync(string? url)
    {
        if (!IsTrustedUrl(url))
        {
            Toast("Blocked untrusted update URL.");
            return;
        }

        bool ok = await UpdateService.DownloadAndApplyUpdateAsync(url!, msg => Toast(msg));
        if (!ok) UpdateService.LaunchUpdate(url);
    }

    private static bool IsTrustedUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
        uri.AbsolutePath.StartsWith($"/{UpdateService.GitHubRepo}/", StringComparison.OrdinalIgnoreCase);

    private static void OpenExternal(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _web.Dispose();
        }
        base.Dispose(disposing);
    }
}
