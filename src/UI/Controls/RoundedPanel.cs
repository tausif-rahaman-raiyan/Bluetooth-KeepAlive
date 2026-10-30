using System.Drawing.Drawing2D;

namespace HocoKeepAlive.UI.Controls;

public class RoundedPanel : Panel
{
    private int _cornerRadius = 14;
    private Color _borderColor = Color.FromArgb(37, 39, 51); // #252733
    private Color? _hoverBorderColor;
    private Color _fillColor = Color.FromArgb(23, 24, 32); // #171820
    private bool _isHovered;

    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Math.Max(0, value);
            Invalidate();
        }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
        }
    }

    public Color? HoverBorderColor
    {
        get => _hoverBorderColor;
        set
        {
            _hoverBorderColor = value;
            Invalidate();
        }
    }

    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            Invalidate();
        }
    }

    public RoundedPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (_hoverBorderColor.HasValue)
        {
            _isHovered = true;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverBorderColor.HasValue)
        {
            _isHovered = false;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        int w = ClientSize.Width;
        int h = ClientSize.Height;
        if (w <= 2 || h <= 2) return;

        float dpiScale = DeviceDpi / 96f;
        int radius = (int)(_cornerRadius * dpiScale);

        var rect = new Rectangle(0, 0, w - 1, h - 1);
        using var path = CreateRoundedRectanglePath(rect, radius);

        using var bgBrush = new SolidBrush(_fillColor);
        g.FillPath(bgBrush, path);

        Color activeBorder = (_isHovered && _hoverBorderColor.HasValue) ? _hoverBorderColor.Value : _borderColor;
        using var borderPen = new Pen(activeBorder, 1.2f);
        g.DrawPath(borderPen, path);
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
