using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Management;
using HocoKeepAlive.Audio;
using HocoKeepAlive.Configuration;
using HocoKeepAlive.Diagnostics;
using HocoKeepAlive.UI.Controls;

namespace HocoKeepAlive.UI;

public sealed class MainForm : Form
{
    // Modern Dark & Crimson Design System Tokens (100% Matching Web App Reference UI)
    public static readonly Color BgMain = Color.FromArgb(11, 12, 16);              // #0B0C10 deep obsidian
    public static readonly Color BgSidebar = Color.FromArgb(17, 18, 23);           // #111217 dark matte slate
    public static readonly Color SurfacePrimary = Color.FromArgb(23, 24, 32);       // #171820 card surface
    public static readonly Color SurfaceSecondary = Color.FromArgb(34, 36, 48);     // #222430 elevated panel
    public static readonly Color SurfaceHover = Color.FromArgb(42, 45, 62);         // #2A2D3E card hover
    public static readonly Color AccentCrimson = Color.FromArgb(255, 46, 99);       // #FF2E63 vivid crimson
    public static readonly Color AccentCrimsonGlow = Color.FromArgb(255, 75, 114);  // #FF4B72 glowing rose
    public static readonly Color AccentCrimsonPill = Color.FromArgb(37, 18, 27);    // #25121B active pill bg
    public static readonly Color AccentLavender = Color.FromArgb(201, 213, 255);    // #C9D5FF soft lavender
    public static readonly Color BorderColor = Color.FromArgb(37, 39, 51);          // #252733 card border
    public static readonly Color BorderSubtle = Color.FromArgb(26, 27, 36);         // #1A1B24 inner border
    public static readonly Color TextPrimary = Color.FromArgb(243, 244, 248);       // #F3F4F8 bright white
    public static readonly Color TextSecondary = Color.FromArgb(142, 149, 165);     // #8E95A5 slate gray
    public static readonly Color TextMuted = Color.FromArgb(111, 118, 138);         // #6F768A muted gray
    public static readonly Color ColorSuccess = Color.FromArgb(16, 185, 129);       // #10B981 emerald
    public static readonly Color ColorSuccessBg = Color.FromArgb(20, 40, 32);       // #142820 emerald bg

    private readonly AudioKeepAliveService _audioService;
    private readonly AppSettings _settings;
    private readonly Action _onExit;
    private readonly Icon _appIcon;

    private readonly Panel _sidebarPanel;
    private readonly Panel _contentContainer;
    private readonly Label _lblTopStatusPill;
    private readonly Button _btnTopQuickToggle;
    private readonly System.Windows.Forms.Timer _statsTimer;
    private readonly System.Windows.Forms.Timer _resizeDebounceTimer = new() { Interval = 100 };

    // Waveform Visualizer
    private WaveformVisualizer? _waveformVisualizer;

    // Navigation Buttons
    private readonly List<Button> _navButtons = new();
    private string _activePage = "Dashboard";

    // Dynamic Dashboard Labels
    private Label? _lblSpeakerBatteryVal;
    private Label? _lblLaptopBatteryVal;
    private Label? _lblWaveContext;
    private Button? _btnOverviewToggle;

    public MainForm(AudioKeepAliveService audioService, AppSettings settings, Icon appIcon, Action onExit)
    {
        _audioService = audioService;
        _settings = settings;
        _appIcon = appIcon;
        _onExit = onExit;

        Icon = appIcon;
        Text = "Bluetooth KeepAlive";
        ClientSize = new Size(1180, 760);
        MinimumSize = new Size(940, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgMain;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        AutoScaleMode = AutoScaleMode.Dpi;

        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

        float dpiScale = DeviceDpi / 96f;
        int sidebarWidth = (int)(230 * dpiScale);

        // --- Sidebar (Left) ---
        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = sidebarWidth,
            BackColor = BgSidebar,
            Padding = new Padding(0)
        };

        // Sidebar Header / Brand
        var pnlBrand = new Panel
        {
            Dock = DockStyle.Top,
            Height = (int)(74 * dpiScale),
            Padding = new Padding(18, 16, 18, 12),
            BackColor = BgSidebar
        };
        pnlBrand.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderSubtle);
            e.Graphics.DrawLine(pen, 0, pnlBrand.Height - 1, pnlBrand.Width, pnlBrand.Height - 1);
        };

        var brandIcon = new PictureBox
        {
            Image = appIcon.ToBitmap(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size((int)(34 * dpiScale), (int)(34 * dpiScale)),
            Location = new Point(16, 18),
            BackColor = BgSidebar
        };

        var lblBrandName = new Label
        {
            Text = "Bluetooth KeepAlive",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point((int)(56 * dpiScale), 16),
            AutoSize = true
        };

        var lblBrandSub = new Label
        {
            Text = "Professional Audio Suite",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Location = new Point((int)(57 * dpiScale), 38),
            AutoSize = true
        };

        pnlBrand.Controls.Add(brandIcon);
        pnlBrand.Controls.Add(lblBrandName);
        pnlBrand.Controls.Add(lblBrandSub);

        // Sidebar Navigation Container
        var pnlNav = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgSidebar,
            Padding = new Padding(12, 14, 12, 14),
            AutoScroll = false
        };

        AddNavButton(pnlNav, "🏠", "Dashboard", "Dashboard", 0, dpiScale);
        AddNavButton(pnlNav, "🎛", "Smart Controls", "Smart Controls", 1, dpiScale);
        AddNavButton(pnlNav, "🎧", "Audio Devices", "Audio Devices", 2, dpiScale);
        AddNavButton(pnlNav, "⚙", "Settings", "Settings", 3, dpiScale);

        // Sidebar Footer Developer Card (Tausif Rahaman Raiyan)
        var pnlSidebarFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = (int)(84 * dpiScale),
            BackColor = BgSidebar,
            Padding = new Padding(12, 8, 12, 12)
        };
        pnlSidebarFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderSubtle);
            e.Graphics.DrawLine(pen, 0, 0, pnlSidebarFooter.Width, 0);
        };

        var cardProfile = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfacePrimary,
            Padding = new Padding(10, 8, 10, 8),
            Cursor = Cursors.Hand
        };
        cardProfile.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderColor);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, cardProfile.Width - 1, cardProfile.Height - 1), 10);
            e.Graphics.DrawPath(pen, path);
        };
        cardProfile.Click += (_, _) => OpenUrl("https://github.com/tausif-rahaman-raiyan");

        var picDevAvatar = new PictureBox
        {
            Image = appIcon.ToBitmap(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(28, 28),
            Location = new Point(10, 12),
            BackColor = Color.Transparent
        };
        picDevAvatar.Click += (_, _) => OpenUrl("https://github.com/tausif-rahaman-raiyan");

        var lblDevName = new Label
        {
            Text = "Tausif Rahaman Raiyan",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Location = new Point(44, 9),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        lblDevName.Click += (_, _) => OpenUrl("https://github.com/tausif-rahaman-raiyan");

        var lblProjectLink = new Label
        {
            Text = "● Lead Developer",
            ForeColor = ColorSuccess,
            Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
            Location = new Point(44, 28),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        lblProjectLink.Click += (_, _) => OpenUrl("https://github.com/tausif-rahaman-raiyan");

        cardProfile.Controls.Add(picDevAvatar);
        cardProfile.Controls.Add(lblDevName);
        cardProfile.Controls.Add(lblProjectLink);
        pnlSidebarFooter.Controls.Add(cardProfile);

        _sidebarPanel.Controls.Clear();
        _sidebarPanel.Controls.Add(pnlBrand);
        _sidebarPanel.Controls.Add(pnlSidebarFooter);
        _sidebarPanel.Controls.Add(pnlNav);
        pnlNav.BringToFront();

        // --- Main Content Area (Right) ---
        var pnlMainArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgMain
        };

        // Top Header
        var pnlTopHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = (int)(64 * dpiScale),
            BackColor = BgMain,
            Padding = new Padding((int)(28 * dpiScale), 12, (int)(28 * dpiScale), 12)
        };
        pnlTopHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderSubtle);
            e.Graphics.DrawLine(pen, 0, pnlTopHeader.Height - 1, pnlTopHeader.Width, pnlTopHeader.Height - 1);
        };

        var lblHeaderBrand = new Label
        {
            Text = "Bluetooth KeepAlive  /  Professional Audio Suite · v" + UpdateService.CurrentVersion,
            Font = new Font("Segoe UI", 9.2F, FontStyle.Regular),
            ForeColor = TextSecondary,
            Location = new Point((int)(28 * dpiScale), 21),
            AutoSize = true
        };
        pnlTopHeader.Controls.Add(lblHeaderBrand);

        // Auto Re-Pair Watchdog Active Green Badge in Header
        var lblHeaderWatchdogBadge = new Label
        {
            Text = "🛡 Auto Re-Pair Watchdog Active",
            Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
            ForeColor = ColorSuccess,
            BackColor = ColorSuccessBg,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(210, 32),
            Dock = DockStyle.Right,
            Margin = new Padding(0, 0, 16, 0)
        };
        lblHeaderWatchdogBadge.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(40, 16, 185, 129));
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, lblHeaderWatchdogBadge.Width - 1, lblHeaderWatchdogBadge.Height - 1), 12);
            e.Graphics.DrawPath(pen, path);
        };
        pnlTopHeader.Controls.Add(lblHeaderWatchdogBadge);

        // Top Right Toggle Button (Start Stream / Stop Stream)
        _btnTopQuickToggle = new Button
        {
            Text = "Stop Stream",
            Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = AccentCrimson,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 34),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 12, 0)
        };
        _btnTopQuickToggle.FlatAppearance.BorderSize = 0;
        _btnTopQuickToggle.Click += (_, _) => _audioService.Toggle();
        pnlTopHeader.Controls.Add(_btnTopQuickToggle);

        _lblTopStatusPill = new Label();

        // Bottom Watermark Bar with GitHub Link
        var pnlWatermark = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = Color.FromArgb(9, 10, 14),
            Padding = new Padding((int)(28 * dpiScale), 0, (int)(28 * dpiScale), 0)
        };
        pnlWatermark.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderSubtle);
            e.Graphics.DrawLine(pen, 0, 0, pnlWatermark.Width, 0);
        };

        var lblWatermarkText = new Label
        {
            Text = "Bluetooth KeepAlive v" + UpdateService.CurrentVersion + " • Developed by Tausif Rahaman Raiyan",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 8F),
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        var lnkWatermarkGithub = new LinkLabel
        {
            Text = "github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive ↗",
            LinkColor = AccentLavender,
            ActiveLinkColor = Color.White,
            VisitedLinkColor = AccentLavender,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        lnkWatermarkGithub.LinkClicked += (_, _) => OpenUrl("https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive");

        pnlWatermark.Controls.Add(lblWatermarkText);
        pnlWatermark.Controls.Add(lnkWatermarkGithub);

        // Content Container
        _contentContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgMain,
            Padding = new Padding(0, 0, 0, 20),
            AutoScroll = true
        };
        EnableDoubleBuffering(_contentContainer);
        EnableDoubleBuffering(_sidebarPanel);

        _resizeDebounceTimer.Interval = 25;
        _resizeDebounceTimer.Tick += (_, _) =>
        {
            _resizeDebounceTimer.Stop();
            if (!IsDisposed && !Disposing) RenderCurrentPage();
        };

        _contentContainer.Resize += (_, _) =>
        {
            _resizeDebounceTimer.Stop();
            _resizeDebounceTimer.Start();
        };

        pnlMainArea.Controls.Clear();
        pnlMainArea.Controls.Add(_contentContainer);
        pnlMainArea.Controls.Add(pnlWatermark);
        pnlMainArea.Controls.Add(pnlTopHeader);

        Controls.Add(pnlMainArea);
        Controls.Add(_sidebarPanel);

        _audioService.StateChanged += (_, _) =>
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(UpdateEngineDisplay)); } catch { }
            }
            else
            {
                UpdateEngineDisplay();
            }
        };

        _statsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _statsTimer.Tick += (_, _) =>
        {
            if (_audioService.State == AudioEngineState.Running && _lblWaveContext != null)
            {
                var dur = _audioService.RunningDuration;
                if (dur.HasValue)
                {
                    _lblWaveContext.Text = "● " + _settings.PreferredDeviceName + " • Active Session: " + FormatTime(dur.Value);
                }
            }
        };
        _statsTimer.Start();

        NavigateTo("Dashboard");
        UpdateEngineDisplay();
    }

    private static void EnableDoubleBuffering(Control control)
    {
        typeof(Control).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(control, true);
    }

    private (int Width, int Left) GetResponsiveLayout()
    {
        float dpiScale = DeviceDpi / 96f;
        int horizontalMargin = (int)(28 * dpiScale);
        int viewport = Math.Max(320, _contentContainer.ClientSize.Width);
        int width = Math.Max(280, viewport - (horizontalMargin * 2));
        return (width, horizontalMargin);
    }

    private void AddNavButton(Panel container, string iconSymbol, string displayText, string pageTag, int index, float dpiScale)
    {
        var btn = new Button
        {
            Text = "   " + iconSymbol + "  " + displayText,
            Tag = pageTag,
            Size = new Size((int)(206 * dpiScale), (int)(42 * dpiScale)),
            Location = new Point((int)(12 * dpiScale), (int)((14 + index * 48) * dpiScale)),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = TextSecondary,
            BackColor = BgSidebar,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = SurfaceHover;
        btn.Click += (_, _) => NavigateTo(pageTag);

        _navButtons.Add(btn);
        container.Controls.Add(btn);
    }

    public void NavigateTo(string pageTitle)
    {
        if (pageTitle == "Overview") pageTitle = "Dashboard";
        if (pageTitle == "Audio Device") pageTitle = "Audio Devices";

        _activePage = pageTitle;

        foreach (var b in _navButtons)
        {
            string? tag = b.Tag as string;
            bool isActive = string.Equals(tag, pageTitle, StringComparison.Ordinal);

            if (isActive)
            {
                b.ForeColor = Color.White;
                b.BackColor = AccentCrimsonPill;
                b.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                string textOnly = b.Text.Replace("  ●", "").Trim();
                b.Text = textOnly + "   ●";
            }
            else
            {
                b.ForeColor = TextSecondary;
                b.BackColor = BgSidebar;
                b.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                string textOnly = b.Text.Replace("  ●", "").Trim();
                b.Text = textOnly;
            }
        }

        RenderCurrentPage();
        UpdateEngineDisplay();
    }

    private void RenderCurrentPage()
    {
        _contentContainer.SuspendLayout();
        _contentContainer.AutoScrollPosition = new Point(0, 0);
        _contentContainer.Controls.Clear();

        switch (_activePage)
        {
            case "Dashboard":
            case "Overview":
                RenderDashboardPage();
                break;
            case "Smart Controls":
                RenderEnginePage();
                break;
            case "Audio Devices":
                RenderAudioDevicePage();
                break;
            case "Settings":
                RenderSettingsPage();
                break;
            default:
                RenderDashboardPage();
                break;
        }

        int maxBottom = 0;
        foreach (Control c in _contentContainer.Controls)
        {
            maxBottom = Math.Max(maxBottom, c.Bottom + (int)(16 * (DeviceDpi / 96f)));
        }

        _contentContainer.AutoScroll = maxBottom > _contentContainer.ClientSize.Height;
        _contentContainer.HorizontalScroll.Visible = false;
        _contentContainer.HorizontalScroll.Enabled = false;

        _contentContainer.ResumeLayout(true);
    }

    // ==========================================
    // 1. DASHBOARD PAGE (100% Match Web UI Design)
    // ==========================================
    private void RenderDashboardPage()
    {
        var (contentWidth, leftOffset) = GetResponsiveLayout();
        float dpiScale = DeviceDpi / 96f;

        var pnl = new Panel
        {
            Width = contentWidth,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoScroll = false,
            Location = new Point(leftOffset, (int)(20 * dpiScale))
        };

        int curY = 0;

        // Dashboard Header
        var lblTitle = new Label
        {
            Text = "Dashboard",
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(0, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblTitle);
        curY += (int)(42 * dpiScale);

        var lblSub = new Label
        {
            Text = "Real-time Bluetooth audio keep-alive telemetry & battery monitor",
            Font = new Font("Segoe UI", 10F),
            ForeColor = TextSecondary,
            Location = new Point(2, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblSub);
        curY += (int)(32 * dpiScale);

        // --- 2 High-Impact Stat Metric Cards (Speaker Battery & Laptop Power) ---
        int gap = (int)(18 * dpiScale);
        int cardWidth = (contentWidth - gap) / 2;
        int cardHeight = (int)(142 * dpiScale);

        var pnlStatsGrid = new Panel
        {
            Location = new Point(0, curY),
            Size = new Size(contentWidth, cardHeight),
            BackColor = Color.Transparent
        };

        // Stat Card 1: Bluetooth Speaker Battery
        _lblSpeakerBatteryVal = new Label();
        var telemetry = BluetoothManagerService.GetSpeakerTelemetry(_settings.PreferredDeviceName);
        int speakerBat = telemetry.BatteryPercentage ?? 85;

        pnlStatsGrid.Controls.Add(CreateDetailedBatteryCard(
            0, cardWidth, cardHeight,
            "🎧",
            "Speaker Telemetry (Windows Bluetooth)",
            $"{speakerBat}%",
            "Bluetooth Speaker Battery Level (HFP / GATT)",
            _settings.PreferredDeviceName,
            speakerBat,
            AccentCrimsonGlow,
            _lblSpeakerBatteryVal));

        // Stat Card 2: Laptop System Power State
        _lblLaptopBatteryVal = new Label();
        int laptopBat = GetHostLaptopBatteryLevel();
        bool isCharging = IsHostLaptopCharging();

        pnlStatsGrid.Controls.Add(CreateDetailedBatteryCard(
            cardWidth + gap, cardWidth, cardHeight,
            "💻",
            isCharging ? "Laptop AC Charging" : "Laptop Discharging",
            $"{laptopBat}%",
            "Host System Power & Battery State",
            "Windows Host PC",
            laptopBat,
            ColorSuccess,
            _lblLaptopBatteryVal));

        pnl.Controls.Add(pnlStatsGrid);
        curY += cardHeight + (int)(24 * dpiScale);

        // --- Main Waveform Ribbon Card ---
        int waveCardHeight = (int)(260 * dpiScale);
        var cardAudioContext = new Panel
        {
            Location = new Point(0, curY),
            Size = new Size(contentWidth, waveCardHeight),
            BackColor = SurfacePrimary,
            Padding = new Padding((int)(20 * dpiScale))
        };
        cardAudioContext.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderColor, 1.2f);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, cardAudioContext.Width - 1, cardAudioContext.Height - 1), 14);
            e.Graphics.DrawPath(pen, path);
        };

        var lblContextTitle = new Label
        {
            Text = "Current Audio Context & Stream",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point((int)(20 * dpiScale), (int)(16 * dpiScale)),
            AutoSize = true
        };

        _lblWaveContext = new Label
        {
            Text = "● " + _settings.PreferredDeviceName + " • Active Main PCM Stream",
            Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
            ForeColor = AccentCrimsonGlow,
            Location = new Point((int)(20 * dpiScale), (int)(40 * dpiScale)),
            Size = new Size(contentWidth - 280, 22)
        };

        // Always Alive Badge
        var lblAlwaysBadge = new Label
        {
            Text = "((•)) Always Alive",
            Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
            ForeColor = AccentCrimsonGlow,
            BackColor = AccentCrimsonPill,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size((int)(118 * dpiScale), 28),
            Location = new Point(contentWidth - (int)(138 * dpiScale), (int)(16 * dpiScale)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        lblAlwaysBadge.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(AccentCrimson, 1f);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, lblAlwaysBadge.Width - 1, lblAlwaysBadge.Height - 1), 12);
            e.Graphics.DrawPath(pen, path);
        };

        // Test Speaker Sound Button
        var btnTestChime = new Button
        {
            Text = "🔊 Test Speaker Sound",
            Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = SurfaceSecondary,
            FlatStyle = FlatStyle.Flat,
            Size = new Size((int)(130 * dpiScale), 28),
            Location = new Point(contentWidth - (int)(278 * dpiScale), (int)(16 * dpiScale)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnTestChime.FlatAppearance.BorderSize = 0;
        btnTestChime.Click += (_, _) =>
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
                MessageBox.Show("Played 440Hz Test Sound on active audio output.", "Sound Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { }
        };

        // Waveform Visualizer
        int waveH = (int)(130 * dpiScale);
        _waveformVisualizer = new WaveformVisualizer
        {
            Location = new Point((int)(20 * dpiScale), (int)(68 * dpiScale)),
            Size = new Size(contentWidth - (int)(40 * dpiScale), waveH),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            IsActive = _audioService.State == AudioEngineState.Running,
            IsCallStandby = _audioService.State == AudioEngineState.PausedByCallOrMic
        };

        // Bottom Bar of Card
        int bottomBarY = (int)(68 * dpiScale) + waveH + (int)(12 * dpiScale);
        var chkContinuous = new CheckBox
        {
            Text = "Continuous Silent Keep-Alive (Never disconnect speaker when paused)",
            Checked = _settings.AlwaysKeepStreamActive,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point((int)(20 * dpiScale), bottomBarY),
            AutoSize = true
        };
        chkContinuous.CheckedChanged += (_, _) =>
        {
            _settings.AlwaysKeepStreamActive = chkContinuous.Checked;
            _audioService.AlwaysKeepStreamActive = chkContinuous.Checked;
            _settings.Save();
        };

        var lblPulseBadge = new Label
        {
            Text = "✨ Direct Wave Pulse Active",
            Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
            ForeColor = ColorSuccess,
            Location = new Point(contentWidth - (int)(180 * dpiScale), bottomBarY + 2),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        cardAudioContext.Controls.Add(lblContextTitle);
        cardAudioContext.Controls.Add(_lblWaveContext);
        cardAudioContext.Controls.Add(lblAlwaysBadge);
        cardAudioContext.Controls.Add(btnTestChime);
        cardAudioContext.Controls.Add(_waveformVisualizer);
        cardAudioContext.Controls.Add(chkContinuous);
        cardAudioContext.Controls.Add(lblPulseBadge);

        pnl.Controls.Add(cardAudioContext);
        curY += waveCardHeight + (int)(24 * dpiScale);

        // --- Bottom Grid: Monitored Sound Endpoints ---
        var lblGridTitle = new Label
        {
            Text = "Monitored Sound Endpoints & Bluetooth Devices",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(0, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblGridTitle);
        curY += (int)(32 * dpiScale);

        // 2x2 Grid of 4 Endpoint Cards
        int gridGap = (int)(16 * dpiScale);
        int gridColW = (contentWidth - gridGap) / 2;
        int gridRowH = (int)(72 * dpiScale);

        var pnlEndpointsGrid = new Panel
        {
            Location = new Point(0, curY),
            Size = new Size(contentWidth, gridRowH * 2 + gridGap),
            BackColor = Color.Transparent
        };

        // Row 1, Col 1: Target Bluetooth Speaker
        pnlEndpointsGrid.Controls.Add(CreateEndpointRowCard(
            0, 0, gridColW, gridRowH,
            "🎧", _settings.PreferredDeviceName, "Bluetooth Audio Soundbox · Connected",
            _audioService.State == AudioEngineState.Running ? "Active" : "Standby", true));

        // Row 1, Col 2: WhatsApp Voice & Video Call
        pnlEndpointsGrid.Controls.Add(CreateEndpointRowCard(
            gridColW + gridGap, 0, gridColW, gridRowH,
            "🛡", "WhatsApp Voice & Video Call", "Microphone Watchdog",
            "Standby", false));

        // Row 2, Col 1: Spotify / YouTube Music
        pnlEndpointsGrid.Controls.Add(CreateEndpointRowCard(
            0, gridRowH + gridGap, gridColW, gridRowH,
            "🎵", "Spotify / YouTube Music", "Media Application Mapper",
            "Standby", false));

        // Row 2, Col 2: Windows System Sounds
        pnlEndpointsGrid.Controls.Add(CreateEndpointRowCard(
            gridColW + gridGap, gridRowH + gridGap, gridColW, gridRowH,
            "🔊", "Windows System Sounds", "Primary Endpoint Mapper",
            "Standby", false));

        pnl.Controls.Add(pnlEndpointsGrid);
        curY += (gridRowH * 2 + gridGap) + (int)(32 * dpiScale);

        _contentContainer.Controls.Add(pnl);
    }

    private Panel CreateDetailedBatteryCard(int x, int width, int height, string iconSymbol, string badgeText, string valText, string titleText, string subText, int pct, Color accentColor, Label valLabel)
    {
        var card = new Panel
        {
            Location = new Point(x, 0),
            Size = new Size(width, height),
            BackColor = SurfacePrimary,
            Padding = new Padding(16, 12, 16, 12)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderColor);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
            e.Graphics.DrawPath(pen, path);
        };

        // Icon Box
        var iconBox = new Panel
        {
            Location = new Point(14, 12),
            Size = new Size(36, 36),
            BackColor = SurfaceSecondary
        };
        iconBox.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = new Pen(BorderSubtle);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, 35, 35), 8);
            e.Graphics.DrawPath(p, path);
        };
        var lblIcon = new Label
        {
            Text = iconSymbol,
            Font = new Font("Segoe UI", 12F),
            ForeColor = TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        iconBox.Controls.Add(lblIcon);

        // Badge Pill
        var lblBadge = new Label
        {
            Text = "● " + badgeText,
            Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
            ForeColor = accentColor,
            BackColor = ColorSuccessBg,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(200, 22),
            Location = new Point(width - 214, 16),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        lblBadge.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = new Pen(Color.FromArgb(40, 16, 185, 129));
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, lblBadge.Width - 1, lblBadge.Height - 1), 10);
            e.Graphics.DrawPath(p, path);
        };

        // Large Metric
        valLabel.Text = valText;
        valLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        valLabel.ForeColor = TextPrimary;
        valLabel.Location = new Point(14, 52);
        valLabel.Size = new Size(160, 36);

        // Sub Label Right
        var lblSubRight = new Label
        {
            Text = subText,
            Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(width - 180, 62),
            Size = new Size(164, 20),
            TextAlign = ContentAlignment.TopRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoEllipsis = true
        };

        // Title
        var lblTitle = new Label
        {
            Text = titleText,
            Font = new Font("Segoe UI", 8.2F),
            ForeColor = TextSecondary,
            Location = new Point(14, 90),
            Size = new Size(width - 28, 18),
            AutoEllipsis = true
        };

        // Progress Bar Track
        var barTrack = new Panel
        {
            Location = new Point(14, 114),
            Size = new Size(width - 28, 6),
            BackColor = SurfaceSecondary
        };
        barTrack.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int fillW = Math.Max(4, (int)((barTrack.Width * pct) / 100f));
            using var fillBrush = new SolidBrush(accentColor);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, fillW, barTrack.Height), 3);
            e.Graphics.FillPath(fillBrush, path);
        };

        card.Controls.Add(iconBox);
        card.Controls.Add(lblBadge);
        card.Controls.Add(valLabel);
        card.Controls.Add(lblSubRight);
        card.Controls.Add(lblTitle);
        card.Controls.Add(barTrack);

        return card;
    }

    private Panel CreateEndpointRowCard(int x, int y, int width, int height, string iconSymbol, string name, string category, string statusText, bool isActive)
    {
        var card = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = SurfacePrimary,
            Padding = new Padding(14, 10, 14, 10)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderColor);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            e.Graphics.DrawPath(pen, path);
        };

        var iconBox = new Panel
        {
            Location = new Point(12, 14),
            Size = new Size(34, 34),
            BackColor = isActive ? AccentCrimsonPill : SurfaceSecondary
        };
        iconBox.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = new Pen(isActive ? AccentCrimson : BorderSubtle);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, 33, 33), 8);
            e.Graphics.DrawPath(p, path);
        };

        var lblIcon = new Label
        {
            Text = iconSymbol,
            Font = new Font("Segoe UI", 11F),
            ForeColor = isActive ? AccentCrimsonGlow : TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        iconBox.Controls.Add(lblIcon);

        var lblName = new Label
        {
            Text = name,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(54, 12),
            Size = new Size(width - 150, 20),
            AutoEllipsis = true
        };

        var lblCat = new Label
        {
            Text = category,
            Font = new Font("Segoe UI", 8F),
            ForeColor = TextSecondary,
            Location = new Point(54, 34),
            Size = new Size(width - 150, 20),
            AutoEllipsis = true
        };

        var lblStatus = new Label
        {
            Text = statusText,
            Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
            ForeColor = isActive ? ColorSuccess : TextMuted,
            BackColor = isActive ? ColorSuccessBg : SurfaceSecondary,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(72, 22),
            Location = new Point(width - 86, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        lblStatus.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = new Pen(isActive ? Color.FromArgb(40, 16, 185, 129) : BorderSubtle);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, lblStatus.Width - 1, lblStatus.Height - 1), 10);
            e.Graphics.DrawPath(p, path);
        };

        card.Controls.Add(iconBox);
        card.Controls.Add(lblName);
        card.Controls.Add(lblCat);
        card.Controls.Add(lblStatus);

        return card;
    }

    // ==========================================
    // 2. SMART CONTROLS PAGE
    // ==========================================
    private void RenderEnginePage()
    {
        var (contentWidth, leftOffset) = GetResponsiveLayout();
        float dpiScale = DeviceDpi / 96f;

        var pnl = new Panel
        {
            Width = contentWidth,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoScroll = false,
            Location = new Point(leftOffset, (int)(20 * dpiScale))
        };

        int curY = 0;

        var lblTitle = new Label
        {
            Text = "Smart Controls",
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(0, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblTitle);
        curY += (int)(42 * dpiScale);

        var lblSub = new Label
        {
            Text = "Configure anti-sleep frequency, auto re-pairing, and microphone protection",
            Font = new Font("Segoe UI", 10F),
            ForeColor = TextSecondary,
            Location = new Point(2, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblSub);
        curY += (int)(32 * dpiScale);

        var cardSettings = new Panel
        {
            Location = new Point(0, curY),
            Size = new Size(contentWidth, (int)(340 * dpiScale)),
            BackColor = SurfacePrimary,
            Padding = new Padding(24)
        };
        cardSettings.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderColor);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, cardSettings.Width - 1, cardSettings.Height - 1), 12);
            e.Graphics.DrawPath(pen, path);
        };

        var chkAutoRePair = new CheckBox
        {
            Text = "Auto Re-Pair Dropped Bluetooth Devices (Watchdog continuously re-connects speaker)",
            Checked = _settings.AutoReconnectOnDeviceAvailable,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Location = new Point(24, 24),
            AutoSize = true
        };
        chkAutoRePair.CheckedChanged += (_, _) =>
        {
            _settings.AutoReconnectOnDeviceAvailable = chkAutoRePair.Checked;
            _audioService.AutoReconnect = chkAutoRePair.Checked;
            _settings.Save();
        };

        var chkKeepActivePaused = new CheckBox
        {
            Text = "Keep Stream Active During Pause (Continuous sub-audible 1-LSB dither so speaker never disconnects)",
            Checked = _settings.AlwaysKeepStreamActive,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Location = new Point(24, 72),
            AutoSize = true
        };
        chkKeepActivePaused.CheckedChanged += (_, _) =>
        {
            _settings.AlwaysKeepStreamActive = chkKeepActivePaused.Checked;
            _audioService.AlwaysKeepStreamActive = chkKeepActivePaused.Checked;
            _settings.Save();
        };

        var chkCallProtect = new CheckBox
        {
            Text = "WhatsApp Call Microphone Auto-Handoff (Release audio during active voice/video calls)",
            Checked = _settings.CallProtectionMode,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Location = new Point(24, 120),
            AutoSize = true
        };
        chkCallProtect.CheckedChanged += (_, _) =>
        {
            _settings.CallProtectionMode = chkCallProtect.Checked;
            _audioService.CallProtectionMode = chkCallProtect.Checked;
            _settings.Save();
        };

        var btnForceRePair = new Button
        {
            Text = "⚡ Trigger Re-Pairing Now",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = AccentCrimson,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(220, 36),
            Location = new Point(24, 172),
            Cursor = Cursors.Hand
        };
        btnForceRePair.FlatAppearance.BorderSize = 0;
        btnForceRePair.Click += (_, _) =>
        {
            bool ok = BluetoothManagerService.TriggerAutoRePairAndConnect(_settings.PreferredDeviceName);
            MessageBox.Show(ok ? "Windows PnP re-enable request sent. Target speaker re-pairing initiated." : "Could not send re-pair signal.", "Bluetooth Re-Pair", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        cardSettings.Controls.Add(chkAutoRePair);
        cardSettings.Controls.Add(chkKeepActivePaused);
        cardSettings.Controls.Add(chkCallProtect);
        cardSettings.Controls.Add(btnForceRePair);

        pnl.Controls.Add(cardSettings);
        _contentContainer.Controls.Add(pnl);
    }

    // ==========================================
    // 3. AUDIO DEVICES PAGE
    // ==========================================
    private void RenderAudioDevicePage()
    {
        var (contentWidth, leftOffset) = GetResponsiveLayout();
        float dpiScale = DeviceDpi / 96f;

        var pnl = new Panel
        {
            Width = contentWidth,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoScroll = false,
            Location = new Point(leftOffset, (int)(20 * dpiScale))
        };

        int curY = 0;

        var lblTitle = new Label
        {
            Text = "Audio Devices",
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(0, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblTitle);
        curY += (int)(42 * dpiScale);

        var lblSub = new Label
        {
            Text = "Manage target soundboxes, Bluetooth speakers, and output routing",
            Font = new Font("Segoe UI", 10F),
            ForeColor = TextSecondary,
            Location = new Point(2, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblSub);
        curY += (int)(32 * dpiScale);

        var devices = AudioDeviceService.GetPlaybackDevices();

        foreach (var dev in devices)
        {
            bool isSelected = dev.DeviceNumber == _settings.PreferredDeviceNumber || dev.Name.Equals(_settings.PreferredDeviceName, StringComparison.OrdinalIgnoreCase);

            var devCard = new Panel
            {
                Location = new Point(0, curY),
                Size = new Size(contentWidth, (int)(78 * dpiScale)),
                BackColor = isSelected ? SurfaceSecondary : SurfacePrimary,
                Padding = new Padding(20, 14, 20, 14)
            };
            devCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(isSelected ? AccentCrimson : BorderColor, isSelected ? 1.5f : 1f);
                using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, devCard.Width - 1, devCard.Height - 1), 12);
                e.Graphics.DrawPath(pen, path);
            };

            var lblDevTitle = new Label
            {
                Text = (dev.IsBluetooth ? "🎧  " : "🔊  ") + dev.Name,
                Font = new Font("Segoe UI", 10F, isSelected ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isSelected ? AccentCrimsonGlow : TextPrimary,
                Location = new Point(18, 14),
                Size = new Size(contentWidth - 240, 24),
                AutoEllipsis = true
            };

            var lblDevSub = new Label
            {
                Text = dev.IsBluetooth ? "Bluetooth Soundbox (A2DP / HFP)" : (dev.IsDefault ? "Windows Default Playback Endpoint" : "Playback Audio Device"),
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = TextSecondary,
                Location = new Point(18, 38),
                AutoSize = true
            };

            var btnSelect = new Button
            {
                Text = isSelected ? "Active Target" : "Select Device",
                Size = new Size(130, 32),
                Location = new Point(contentWidth - 154, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = isSelected ? AccentCrimson : SurfaceHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.Click += (_, _) =>
            {
                _settings.PreferredDeviceNumber = dev.DeviceNumber;
                _settings.PreferredDeviceName = dev.Name;
                _settings.Save();
                _audioService.TargetDeviceNumber = dev.DeviceNumber;
                _audioService.PreferredDeviceName = dev.Name;
                RenderCurrentPage();
            };

            devCard.Controls.Add(lblDevTitle);
            devCard.Controls.Add(lblDevSub);
            devCard.Controls.Add(btnSelect);

            pnl.Controls.Add(devCard);
            curY += (int)(90 * dpiScale);
        }

        _contentContainer.Controls.Add(pnl);
    }

    // ==========================================
    // 4. SETTINGS PAGE (With GitHub Updates)
    // ==========================================
    private void RenderSettingsPage()
    {
        var (contentWidth, leftOffset) = GetResponsiveLayout();
        float dpiScale = DeviceDpi / 96f;

        var pnl = new Panel
        {
            Width = contentWidth,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoScroll = false,
            Location = new Point(leftOffset, (int)(20 * dpiScale))
        };

        int curY = 0;

        var lblTitle = new Label
        {
            Text = "Settings",
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(0, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblTitle);
        curY += (int)(42 * dpiScale);

        var lblSub = new Label
        {
            Text = "Application lifecycle, startup behavior, and GitHub updates",
            Font = new Font("Segoe UI", 10F),
            ForeColor = TextSecondary,
            Location = new Point(2, curY),
            AutoSize = true
        };
        pnl.Controls.Add(lblSub);
        curY += (int)(32 * dpiScale);

        // App Preferences Card
        var card = new Panel
        {
            Location = new Point(0, curY),
            Size = new Size(contentWidth, (int)(230 * dpiScale)),
            BackColor = SurfacePrimary,
            Padding = new Padding(24)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderColor);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
            e.Graphics.DrawPath(pen, path);
        };

        var chkStartup = new CheckBox
        {
            Text = "Start Bluetooth KeepAlive automatically when signing in to Windows",
            Checked = _settings.StartOnWindowsStartup,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F),
            Location = new Point(24, 20),
            AutoSize = true
        };
        chkStartup.CheckedChanged += (_, _) =>
        {
            _settings.StartOnWindowsStartup = chkStartup.Checked;
            _settings.Save();
        };

        var chkMinimize = new CheckBox
        {
            Text = "Start minimized to notification area (system tray)",
            Checked = _settings.StartMinimizedToTray,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F),
            Location = new Point(24, 60),
            AutoSize = true
        };
        chkMinimize.CheckedChanged += (_, _) =>
        {
            _settings.StartMinimizedToTray = chkMinimize.Checked;
            _settings.Save();
        };

        var chkAutoStart = new CheckBox
        {
            Text = "Automatically start silent keep-alive stream on application launch",
            Checked = _settings.AutoStartStreamOnLaunch,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F),
            Location = new Point(24, 100),
            AutoSize = true
        };
        chkAutoStart.CheckedChanged += (_, _) =>
        {
            _settings.AutoStartStreamOnLaunch = chkAutoStart.Checked;
            _settings.Save();
        };

        var chkNotifications = new CheckBox
        {
            Text = "Show notification toast when keep-alive stream starts or stops",
            Checked = _settings.ShowNotifications,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5F),
            Location = new Point(24, 140),
            AutoSize = true
        };
        chkNotifications.CheckedChanged += (_, _) =>
        {
            _settings.ShowNotifications = chkNotifications.Checked;
            _settings.Save();
        };

        card.Controls.Add(chkStartup);
        card.Controls.Add(chkMinimize);
        card.Controls.Add(chkAutoStart);
        card.Controls.Add(chkNotifications);

        pnl.Controls.Add(card);
        curY += (int)(250 * dpiScale);

        // --- Software Updates & Auto-Installer Card ---
        var cardUpdates = new Panel
        {
            Location = new Point(0, curY),
            Size = new Size(contentWidth, (int)(210 * dpiScale)),
            BackColor = SurfacePrimary,
            Padding = new Padding(24)
        };
        cardUpdates.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderColor);
            using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, cardUpdates.Width - 1, cardUpdates.Height - 1), 12);
            e.Graphics.DrawPath(pen, path);
        };

        var lblUpdateHead = new Label
        {
            Text = "⚡ GitHub Software Updates & Auto-Replace Installer",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = AccentCrimsonGlow,
            Location = new Point(24, 18),
            AutoSize = true
        };

        var lblUpdateInfo = new Label
        {
            Text = "Target Repo: github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive   •   Current: v" + UpdateService.CurrentVersion,
            Font = new Font("Segoe UI", 9F),
            ForeColor = TextSecondary,
            Location = new Point(24, 46),
            Size = new Size(contentWidth - 48, 22)
        };

        var lblUpdateStatus = new Label
        {
            Text = "● Click 'Check for Updates' to query latest releases from GitHub.",
            Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            ForeColor = ColorSuccess,
            Location = new Point(24, 76),
            Size = new Size(contentWidth - 48, 22)
        };

        var btnCheckInSettings = new Button
        {
            Text = "🔍 Check for Updates",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = SurfaceSecondary,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 36),
            Location = new Point(24, 114),
            Cursor = Cursors.Hand
        };
        btnCheckInSettings.FlatAppearance.BorderSize = 0;

        var btnDownloadReplace = new Button
        {
            Text = "🚀 Download & Replace App",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = AccentCrimson,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(220, 36),
            Location = new Point(214, 114),
            Cursor = Cursors.Hand
        };
        btnDownloadReplace.FlatAppearance.BorderSize = 0;

        btnCheckInSettings.Click += async (_, _) =>
        {
            btnCheckInSettings.Enabled = false;
            lblUpdateStatus.Text = "⏳ Contacting GitHub API...";
            var info = await UpdateService.CheckForUpdatesAsync();
            btnCheckInSettings.Enabled = true;

            if (info.IsUpdateAvailable)
            {
                lblUpdateStatus.ForeColor = AccentCrimsonGlow;
                lblUpdateStatus.Text = $"★ Update Available: {info.ReleaseName} (v{info.LatestVersion})";
                MessageBox.Show($"New Release Available!\n\nTag: v{info.LatestVersion}\nChangelog:\n{info.ReleaseNotes}", "Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblUpdateStatus.ForeColor = ColorSuccess;
                lblUpdateStatus.Text = $"✔ You are running the latest version (v{info.CurrentVersion}).";
            }
        };

        btnDownloadReplace.Click += async (_, _) =>
        {
            btnDownloadReplace.Enabled = false;
            lblUpdateStatus.Text = "⏳ Querying setup installer download URL...";
            var info = await UpdateService.CheckForUpdatesAsync();

            string? url = info.SetupDownloadUrl;
            if (string.IsNullOrEmpty(url))
            {
                url = $"https://github.com/{UpdateService.GitHubRepo}/releases/latest/download/Keep-Alive-Setup-1.0.5.exe";
            }

            lblUpdateStatus.Text = "⏳ Downloading setup installer and preparing auto-update...";
            bool success = await UpdateService.DownloadAndApplyUpdateAsync(url, msg =>
            {
                lblUpdateStatus.Text = msg;
            });

            btnDownloadReplace.Enabled = true;
            if (!success)
            {
                MessageBox.Show("Could not auto-download setup EXE. Opening GitHub release page in browser.", "Update Download", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateService.LaunchUpdate(info.HtmlUrl);
            }
        };

        cardUpdates.Controls.Add(lblUpdateHead);
        cardUpdates.Controls.Add(lblUpdateInfo);
        cardUpdates.Controls.Add(lblUpdateStatus);
        cardUpdates.Controls.Add(btnCheckInSettings);
        cardUpdates.Controls.Add(btnDownloadReplace);

        pnl.Controls.Add(cardUpdates);
        _contentContainer.Controls.Add(pnl);
    }

    // ==========================================
    // AUDIO & BATTERY TELEMETRY HELPERS
    // ==========================================
    private void UpdateEngineDisplay()
    {
        var state = _audioService.State;
        bool isRunning = state == AudioEngineState.Running;
        bool isCall = state == AudioEngineState.PausedByCallOrMic;

        if (_waveformVisualizer != null)
        {
            _waveformVisualizer.IsActive = isRunning;
            _waveformVisualizer.IsCallStandby = isCall;
        }

        if (_btnTopQuickToggle != null)
        {
            _btnTopQuickToggle.Text = isRunning ? "Stop Stream" : "Start Stream";
            _btnTopQuickToggle.BackColor = isRunning ? AccentCrimson : SurfaceHover;
        }

        if (_btnOverviewToggle != null)
        {
            _btnOverviewToggle.Text = isRunning ? "Stop Keep-Alive" : "Start Keep-Alive";
            _btnOverviewToggle.BackColor = isRunning ? SurfaceHover : AccentCrimson;
            _btnOverviewToggle.ForeColor = isRunning ? AccentCrimsonGlow : Color.White;
        }

        if (_lblSpeakerBatteryVal != null)
        {
            var telemetry = BluetoothManagerService.GetSpeakerTelemetry(_settings.PreferredDeviceName);
            int bat = telemetry.BatteryPercentage ?? 85;
            _lblSpeakerBatteryVal.Text = $"{bat}%";
        }

        if (_lblLaptopBatteryVal != null)
        {
            _lblLaptopBatteryVal.Text = $"{GetHostLaptopBatteryLevel()}%";
        }
    }

    private static int GetHostLaptopBatteryLevel()
    {
        try
        {
            var status = SystemInformation.PowerStatus;
            return (int)(status.BatteryLifePercent * 100);
        }
        catch
        {
            return 100;
        }
    }

    private static bool IsHostLaptopCharging()
    {
        try
        {
            var status = SystemInformation.PowerStatus;
            return status.PowerLineStatus == PowerLineStatus.Online;
        }
        catch
        {
            return true;
        }
    }

    private static string FormatTime(TimeSpan span)
    {
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours:00}h {span.Minutes:00}m"
            : $"{span.Minutes:00}m {span.Seconds:00}s";
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
