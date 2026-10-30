using System.Runtime.InteropServices;
using HocoKeepAlive.Diagnostics;

namespace HocoKeepAlive.Services;

public sealed class HotKeyManager : NativeWindow, IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

    // Modifiers
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;

    private bool _isRegistered;
    public event Action? HotKeyPressed;

    public HotKeyManager()
    {
        CreateHandle(new CreateParams());
    }

    public bool Register(uint modifiers = MOD_CONTROL | MOD_ALT, uint key = (uint)Keys.K)
    {
        if (_isRegistered) Unregister();

        _isRegistered = RegisterHotKey(Handle, HOTKEY_ID, modifiers, key);
        if (_isRegistered)
        {
            LogService.Log(LogSeverity.Info, "HotKey", "Global hotkey registered: Ctrl+Alt+K");
        }
        else
        {
            LogService.Log(LogSeverity.Warning, "HotKey", "Failed to register global hotkey Ctrl+Alt+K.");
        }

        return _isRegistered;
    }

    public void Unregister()
    {
        if (_isRegistered)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            _isRegistered = false;
            LogService.Log(LogSeverity.Info, "HotKey", "Global hotkey unregistered.");
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            LogService.Log(LogSeverity.Info, "HotKey", "Global hotkey pressed: Toggling Keep Alive stream state.");
            HotKeyPressed?.Invoke();
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }
}
