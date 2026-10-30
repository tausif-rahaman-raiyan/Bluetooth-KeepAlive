namespace HocoKeepAlive.Services;

public static class LocalizationManager
{
    private static string _currentLanguage = "en";

    public static event Action? LanguageChanged;

    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value == "bn" ? "bn" : "en";
                LanguageChanged?.Invoke();
            }
        }
    }

    private static readonly Dictionary<string, (string En, string Bn)> Translations = new()
    {
        // Header & Navigation
        ["AppTitle"] = ("Bluetooth KeepAlive", "ব্লুটুথ কিপ-এলাইভ"),
        ["Subtitle"] = ("Professional Audio Suite", "প্রফেশনাল অডিও স্যুট"),
        ["TabDashboard"] = ("Dashboard", "ড্যাশবোর্ড"),
        ["TabSmartControls"] = ("Smart Controls", "স্মার্ট কন্ট্রোল"),
        ["TabAudioDevices"] = ("Audio Devices", "অডিও ডিভাইস"),
        ["TabSettings"] = ("Settings", "সেটিংস"),
        ["TabDiagnostics"] = ("Diagnostics & Logs", "ডায়াগনস্টিকস ও লগ"),
        ["BtnStopKeepAlive"] = ("Stop Keep-Alive", "কিপ-এলাইভ বন্ধ করুন"),
        ["BtnStartKeepAlive"] = ("Start Keep-Alive", "কিপ-এলাইভ চালু করুন"),

        // Dashboard Labels
        ["DashboardHeader"] = ("Dashboard", "ড্যাশবোর্ড"),
        ["DashboardSub"] = ("Real-time Bluetooth audio keep-alive telemetry & battery monitor", "রিয়েল-টাইম ব্লুটুথ অডিও কিপ-এলাইভ ও ব্যাটারি মনিটর"),
        ["SpeakerBattery"] = ("Speaker Battery Level", "স্পিকারের ব্যাটারি লেভেল"),
        ["HostBattery"] = ("Host System Power & Battery State", "হোস্ট সিস্টেমের পাওয়ার ও ব্যাটারি স্টেট"),
        ["CurrentContext"] = ("Current Audio Context & Stream", "বর্তমান অডিও কনটেক্সট ও স্ট্রিম"),
        ["WatchdogActive"] = ("Auto Re-Pair Watchdog Active", "অটো রি-পেয়ার ওয়াচডগ সক্রিয়"),
        ["SpeakerTelemetryBadge"] = ("Speaker Telemetry (Windows Bluetooth)", "স্পিকার টেলিমেট্রি (উইন্ডোজ ব্লুটুথ)"),
        ["HostPowerBadge"] = ("Laptop AC / Battery State", "ল্যাপটপ এসি / ব্যাটারি স্টেট"),
        ["WirelessSpeaker"] = ("Wireless Bluetooth Speaker", "ওয়্যারলেস ব্লুটুথ স্পিকার"),
        ["TestSpeakerSound"] = ("Test Speaker Sound", "স্পিকার সাউন্ড টেস্ট করুন"),
        ["AlwaysAliveBadge"] = ("Always Alive", "অলওয়েজ এলাইভ"),

        // Bottom Stat Cards
        ["BluetoothSoundbox"] = ("Bluetooth Soundbox", "ব্লুটুথ সাউন্ডবক্স"),
        ["AutoHandoff"] = ("WhatsApp Voice Call Protection", "হোয়াটসঅ্যাপ কল প্রোটেকশন"),
        ["AutoHandoffSub"] = ("Releases audio instantly during calls to unblock microphone", "কল শুরু হলে মাইক্রোফোন আনব্লক করতে অডিও রিলিজ করে"),
        ["EngineControl"] = ("Keep-Alive Engine Control", "কিপ-এলাইভ ইঞ্জিন কন্ট্রোল"),
        ["PrioritizeSoundbox"] = ("Prioritize BT Soundbox", "বিটি সাউন্ডবক্স প্র অগ্রাধিকার দিন"),
        ["TestCallStandby"] = ("Test Call Standby", "কল স্ট্যান্ডবাই টেস্ট করুন"),

        // Smart Controls / Settings
        ["AutoStartOnConnect"] = ("Auto-Start Stream on Speaker Connect", "স্পিকার কানেক্ট হলে স্বয়ংক্রিয়ভাবে স্ট্রিম শুরু করুন"),
        ["AntiSleepDither"] = ("Anti-Sleep 1-LSB Dither Mode", "অ্যান্টি-স্লিপ ১-এলএসবি ডিদার মোড"),
        ["GlobalHotkey"] = ("Global Hotkey (Ctrl+Alt+K to Toggle)", "গ্লোবাল হটকি (Ctrl+Alt+K দিয়ে অন/অফ)"),
        ["LowBatteryAlert"] = ("Notify when Speaker Battery < 20%", "স্পিকারের ব্যাটারি ২০% এর নিচে নামলে অ্যালার্ট দিন"),
        ["ActiveSchedule"] = ("Schedule Active Hours (e.g. 20:00 - 24:00)", "নির্ধারিত সময়ে সক্রিয় রাখুন (যেমন রাত ৮টা - ১২টা)"),
        ["StartOnBoot"] = ("Start Automatically on Windows Sign-in", "উইন্ডোজ চালু হওয়ার সাথে সাথে অটো-স্টার্ট করুন"),
        ["MinimizeToTray"] = ("Minimize to System Tray on Close", "বন্ধ করলে সিস্টেম ট্রেতে মিনিমাইজ রাখুন"),
        ["LanguageToggle"] = ("UI Language / ভাষার ধরণ", "UI Language / ভাষার ধরণ"),

        // Diagnostics
        ["WhyDisconnected"] = ("Why did my speaker disconnect?", "কেন আমার স্পিকার ডিসকানেক্ট হলো?"),
        ["LogViewerHeader"] = ("Live Diagnostic Session Logs", "লাইভ ডায়াগনস্টিক সেশন লগ"),
        ["ExportLogs"] = ("Export Diagnostic Report", "ডায়াগনস্টিক রিপোর্ট এক্সপোর্ট করুন"),
        ["ClearLogs"] = ("Clear Session Logs", "সেশন লগ মুছুন"),

        // Developer Info
        ["DeveloperName"] = ("Tausif Rahaman Raiyan", "তৌসিফ রহমান রায়ান"),
        ["DeveloperRole"] = ("Lead Developer", "লিড ডেভেলপমেন্ট"),
    };

    public static string Get(string key)
    {
        if (Translations.TryGetValue(key, out var pair))
        {
            return _currentLanguage == "bn" ? pair.Bn : pair.En;
        }
        return key;
    }
}
