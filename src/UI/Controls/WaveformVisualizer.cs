using System.Drawing.Drawing2D;

namespace HocoKeepAlive.UI.Controls;

public sealed class WaveformVisualizer : Control
{
    private readonly System.Windows.Forms.Timer _animTimer;
    private float _phase;
    private bool _isActive = true;
    private bool _isCallStandby;
    private string _deviceName = "Wireless Bluetooth Speaker";

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                Invalidate();
            }
        }
    }

    public bool IsCallStandby
    {
        get => _isCallStandby;
        set
        {
            if (_isCallStandby != value)
            {
                _isCallStandby = value;
                Invalidate();
            }
        }
    }

    public string DeviceName
    {
        get => _deviceName;
        set
        {
            _deviceName = value;
            Invalidate();
        }
    }

    public WaveformVisualizer()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        DoubleBuffered = true;
        BackColor = Color.FromArgb(11, 12, 16); // #0B0C10 deep dark inset

        _animTimer = new System.Windows.Forms.Timer { Interval = 25 }; // 40 FPS smooth canvas loop
        _animTimer.Tick += (_, _) =>
        {
            _phase += _isActive ? 0.035f : 0.012f;
            if (_phase > MathF.PI * 8f)
            {
                _phase -= MathF.PI * 8f;
            }
            Invalidate();
        };
        _animTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animTimer.Stop();
            _animTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        int w = ClientSize.Width;
        int h = ClientSize.Height;
        if (w <= 10 || h <= 10) return;

        // Dark rounded canvas background (#0B0C10)
        using var bgBrush = new SolidBrush(Color.FromArgb(11, 12, 16));
        using var borderPen = new Pen(Color.FromArgb(34, 36, 50), 1f);
        using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, w - 1, h - 1), 12);
        g.FillPath(bgBrush, path);
        g.DrawPath(borderPen, path);

        float midY = h * 0.52f;

        // Subtle horizontal center grid line
        using (var guidePen = new Pen(Color.FromArgb(29, 30, 40), 1f) { DashStyle = DashStyle.Dash })
        {
            g.DrawLine(guidePen, 20, midY, w - 20, midY);
        }

        if (_isCallStandby)
        {
            // WhatsApp Call Standby Wave
            float pulse = (MathF.Sin(_phase * 2f) + 1f) * 0.5f;
            int waveAlpha = (int)(100 + pulse * 120);

            using var callGlowPen = new Pen(Color.FromArgb(waveAlpha / 3, 239, 68, 68), 6f);
            using var callPen = new Pen(Color.FromArgb(waveAlpha, 251, 146, 60), 2.5f);
            DrawSinWave(g, callGlowPen, w, midY, 18f, 0.015f, _phase);
            DrawSinWave(g, callPen, w, midY, 18f, 0.015f, _phase);

            string callMsg = "● WHATSAPP VIDEO CALL ACTIVE · MICROPHONE FREED FOR TALKING";
            using var font = new Font("Segoe UI", 8.8F, FontStyle.Bold);
            var sz = g.MeasureString(callMsg, font);
            var pillRect = new RectangleF((w - sz.Width - 28) / 2f, midY - 14, sz.Width + 28, 28);

            using var pillBg = new SolidBrush(Color.FromArgb(220, 36, 20, 26));
            using var pillBorder = new Pen(Color.FromArgb(239, 68, 68), 1.2f);
            using var pillPath = CreateRoundedRectanglePath(Rectangle.Round(pillRect), 14);
            g.FillPath(pillBg, pillPath);
            g.DrawPath(pillBorder, pillPath);

            using var textBrush = new SolidBrush(Color.FromArgb(254, 202, 202));
            g.DrawString(callMsg, font, textBrush, pillRect.X + 14, pillRect.Y + 6);
            return;
        }

        // Base amplitude
        float baseAmp = _isActive ? Math.Clamp(h * 0.28f, 22f, 48f) : 10f;

        // Draw 5 distinct flowing multi-layered sinusoidal ribbon waves with ambient glow underlay
        // Layer 0: Deep Ambient Glow Underlay
        Color glowUnder = _isActive ? Color.FromArgb(30, 255, 46, 99) : Color.FromArgb(15, 16, 185, 129);
        using (var glowPen = new Pen(glowUnder, 16f))
        {
            DrawCompoundSinWave(g, glowPen, w, midY, baseAmp * 1.25f, 0.005f, 0.012f, 1.0f, _phase, 0);
        }

        // Layer 1: Crimson Base Atmosphere Wave
        Color color1 = _isActive ? Color.FromArgb(60, 255, 46, 99) : Color.FromArgb(30, 16, 185, 129);
        using (var pen1 = new Pen(color1, 7f))
        {
            DrawCompoundSinWave(g, pen1, w, midY, baseAmp * 1.15f, 0.005f, 0.012f, 1.0f, _phase, 0);
        }

        // Layer 2: Secondary Vivid Pink Wave
        Color color2 = _isActive ? Color.FromArgb(160, 255, 75, 114) : Color.FromArgb(100, 16, 185, 129);
        using (var pen2 = new Pen(color2, 4f))
        {
            DrawCompoundSinWave(g, pen2, w, midY, baseAmp * 0.95f, 0.007f, 0.015f, 1.25f, _phase, 1);
        }

        // Layer 3: Deep Coral / Rose Wave
        Color color3 = _isActive ? Color.FromArgb(210, 230, 57, 86) : Color.FromArgb(160, 52, 211, 153);
        using (var pen3 = new Pen(color3, 3f))
        {
            DrawCompoundSinWave(g, pen3, w, midY, baseAmp * 0.8f, 0.006f, 0.018f, 0.85f, _phase, 2);
        }

        // Layer 4: Top Focal Bright Pink Ribbon
        Color color4 = _isActive ? Color.FromArgb(245, 255, 130, 160) : Color.FromArgb(220, 110, 231, 183);
        using (var pen4 = new Pen(color4, 2.2f))
        {
            DrawCompoundSinWave(g, pen4, w, midY, baseAmp * 0.65f, 0.009f, 0.022f, 1.4f, _phase, 3);
        }

        // Overlay Badge Top Left
        using var statusFont = new Font("Segoe UI", 8F, FontStyle.Bold);
        using var statusBrush = new SolidBrush(_isActive ? Color.FromArgb(240, 255, 75, 114) : Color.FromArgb(240, 16, 185, 129));
        string statusText = _isActive ? "● 48,000 HZ STEREO KEEP-ALIVE" : "● BACKGROUND ANTI-DISCONNECT ACTIVE";
        g.DrawString(statusText, statusFont, statusBrush, 16, 12);

        // Overlay Info Top Right
        using var rightFont = new Font("Segoe UI", 8F);
        using var rightBrush = new SolidBrush(Color.FromArgb(142, 149, 165));
        string rightText = _deviceName + " • Anti-Sleep 1-LSB Dither Enabled";
        var rSize = g.MeasureString(rightText, rightFont);
        g.DrawString(rightText, rightFont, rightBrush, w - rSize.Width - 16, 12);
    }

    private static void DrawSinWave(Graphics g, Pen pen, int width, float midY, float amp, float freq, float phase)
    {
        var points = new List<PointF>(width / 4 + 2);
        for (int x = 16; x <= width - 16; x += 4)
        {
            float y = midY + MathF.Sin(x * freq + phase) * amp;
            points.Add(new PointF(x, y));
        }

        if (points.Count > 1)
        {
            g.DrawCurve(pen, points.ToArray(), 0.5f);
        }
    }

    private static void DrawCompoundSinWave(Graphics g, Pen pen, int width, float midY, float amp, float freq1, float freq2, float speed, float phase, int idx)
    {
        var points = new List<PointF>(width / 4 + 2);
        for (int x = 16; x <= width - 16; x += 4)
        {
            float normX = x;
            float y = midY + (MathF.Sin(normX * freq1 + phase * speed + idx) * 0.65f + MathF.Sin(normX * freq2 - phase * speed * 0.8f + idx * 2) * 0.35f) * amp;
            points.Add(new PointF(x, y));
        }

        if (points.Count > 1)
        {
            g.DrawCurve(pen, points.ToArray(), 0.5f);
        }
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
