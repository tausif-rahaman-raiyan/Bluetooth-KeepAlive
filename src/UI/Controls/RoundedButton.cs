using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace HocoKeepAlive.UI.Controls;

public enum ButtonStyle
{
    Primary,
    Secondary
}

public class RoundedButton : Button
{
    private int _cornerRadius = 10;
    private ButtonStyle _buttonStyle = ButtonStyle.Primary;
    private bool _isHovered;
    private bool _isPressed;

    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Math.Max(0, value);
            Invalidate();
        }
    }

    public ButtonStyle ButtonStyle
    {
        get => _buttonStyle;
        set
        {
            _buttonStyle = value;
            Invalidate();
        }
    }

    protected override bool ShowFocusCues => false;

    public RoundedButton()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        int w = ClientSize.Width;
        int h = ClientSize.Height;
        if (w <= 2 || h <= 2) return;

        float dpiScale = DeviceDpi / 96f;
        int radius = (int)(_cornerRadius * dpiScale);

        Color fill;
        Color border;

        if (_buttonStyle == ButtonStyle.Primary)
        {
            if (!Enabled)
            {
                fill = Color.FromArgb(120, 255, 46, 99);
                border = Color.Transparent;
            }
            else if (_isPressed)
            {
                fill = Color.FromArgb(200, 30, 70); // #C81E46
                border = Color.FromArgb(255, 46, 99);
            }
            else if (_isHovered)
            {
                fill = Color.FromArgb(255, 59, 105); // #FF3B69
                border = Color.FromArgb(255, 75, 114);
            }
            else
            {
                fill = Color.FromArgb(255, 46, 99); // #FF2E63
                border = Color.Transparent;
            }
        }
        else // Secondary
        {
            if (!Enabled)
            {
                fill = Color.FromArgb(20, 22, 30);
                border = Color.FromArgb(30, 32, 42);
            }
            else if (_isPressed)
            {
                fill = Color.FromArgb(24, 26, 36);
                border = Color.FromArgb(42, 45, 62);
            }
            else if (_isHovered)
            {
                fill = Color.FromArgb(42, 45, 62); // SurfaceHover
                border = Color.FromArgb(60, 64, 88);
            }
            else
            {
                fill = Color.FromArgb(34, 36, 48); // SurfaceSecondary (#222430)
                border = Color.FromArgb(45, 48, 64);
            }
        }

        var rect = new Rectangle(0, 0, w - 1, h - 1);
        using var path = CreateRoundedRectanglePath(rect, radius);

        using var bgBrush = new SolidBrush(fill);
        g.FillPath(bgBrush, path);

        if (border != Color.Transparent)
        {
            using var borderPen = new Pen(border, 1f);
            g.DrawPath(borderPen, path);
        }

        if (!string.IsNullOrEmpty(Text))
        {
            TextRenderer.DrawText(g, Text, Font, rect, Enabled ? ForeColor : Color.FromArgb(140, 145, 160),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        int d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
