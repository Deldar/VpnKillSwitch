using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace VpnKillSwitch;

internal enum AppPage
{
    Dashboard,
    Applications,
    Tunnel,
    Sensitivity,
    Diagnostics,
    Info
}

internal enum VectorIcon
{
    Dashboard,
    Applications,
    Endpoints,
    Sensitivity,
    Activity,
    Info,
    Play,
    Trash,
    Power,
    Sun,
    Moon,
    Warning,
    Check
}

internal static class PageLayout
{
    public static void Compose(Control parent, Control fill, Control[] topRows, Control[] bottomRows)
    {
        parent.Controls.Add(fill);
        foreach (Control row in bottomRows)
            parent.Controls.Add(row);
        for (int i = topRows.Length - 1; i >= 0; i--)
            parent.Controls.Add(topRows[i]);
    }

    public static void Compose(Control parent, Control fill, params Control[] topRows) =>
        Compose(parent, fill, topRows, []);
}

internal static class VectorIcons
{
    public static void Draw(Graphics g, VectorIcon icon, Point p, Color color, float size = 18f)
    {
        float s = size / 18f;
        using var pen = new Pen(color, 1.6f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(color);

        switch (icon)
        {
            case VectorIcon.Dashboard:
                foreach (var (dx, dy) in new[] { (0f, 0f), (10f, 0f), (0f, 10f), (10f, 10f) })
                {
                    using var sq = UiTheme.RoundedRect(
                        new Rectangle(p.X + (int)(dx * s), p.Y + (int)(dy * s), (int)(8 * s), (int)(8 * s)), 2);
                    g.FillPath(brush, sq);
                }
                break;

            case VectorIcon.Applications:
                using (var path = UiTheme.RoundedRect(
                    new Rectangle(p.X, p.Y + (int)(1 * s), (int)(18 * s), (int)(15 * s)), 3))
                {
                    g.DrawPath(pen, path);
                }
                g.DrawLine(pen, p.X, p.Y + 6 * s, p.X + 18 * s, p.Y + 6 * s);
                g.DrawLine(pen, p.X + 4 * s, p.Y + 11 * s, p.X + 10 * s, p.Y + 11 * s);
                break;

            case VectorIcon.Endpoints:
                g.DrawEllipse(pen, p.X, p.Y + 1 * s, 18 * s, 16 * s);
                g.DrawEllipse(pen, p.X + 6 * s, p.Y + 1 * s, 6 * s, 16 * s);
                g.DrawLine(pen, p.X, p.Y + 9 * s, p.X + 18 * s, p.Y + 9 * s);
                break;

            case VectorIcon.Sensitivity:
                foreach (var (dy, kx) in new[] { (3f, 12f), (9f, 5f), (15f, 14f) })
                {
                    g.DrawLine(pen, p.X, p.Y + dy * s, p.X + 18 * s, p.Y + dy * s);
                    g.FillEllipse(brush, p.X + (kx - 2) * s, p.Y + (dy - 2) * s, 5 * s, 5 * s);
                }
                break;

            case VectorIcon.Activity:
                g.DrawLines(pen, [
                    new PointF(p.X + 0.5f * s, p.Y + 9 * s),
                    new PointF(p.X + 5 * s, p.Y + 9 * s),
                    new PointF(p.X + 7.5f * s, p.Y + 3 * s),
                    new PointF(p.X + 10.5f * s, p.Y + 15 * s),
                    new PointF(p.X + 13 * s, p.Y + 9 * s),
                    new PointF(p.X + 17.5f * s, p.Y + 9 * s)
                ]);
                break;

            case VectorIcon.Play:
                g.FillPolygon(brush, [
                    new PointF(p.X + 3 * s, p.Y + 1 * s),
                    new PointF(p.X + 15 * s, p.Y + 8 * s),
                    new PointF(p.X + 3 * s, p.Y + 15 * s)
                ]);
                break;

            case VectorIcon.Trash:
                g.DrawLine(pen, p.X + 2 * s, p.Y + 4.5f * s, p.X + 16 * s, p.Y + 4.5f * s);
                g.DrawLine(pen, p.X + 7 * s, p.Y + 4.5f * s, p.X + 7 * s, p.Y + 1.5f * s);
                g.DrawLine(pen, p.X + 7 * s, p.Y + 1.5f * s, p.X + 11 * s, p.Y + 1.5f * s);
                g.DrawLine(pen, p.X + 11 * s, p.Y + 1.5f * s, p.X + 11 * s, p.Y + 4.5f * s);
                g.DrawLines(pen, [
                    new PointF(p.X + 4.8f * s, p.Y + 4.5f * s),
                    new PointF(p.X + 5.8f * s, p.Y + 16.5f * s),
                    new PointF(p.X + 12.2f * s, p.Y + 16.5f * s),
                    new PointF(p.X + 13.2f * s, p.Y + 4.5f * s)
                ]);
                g.DrawLine(pen, p.X + 7.6f * s, p.Y + 7.5f * s, p.X + 8.1f * s, p.Y + 13.5f * s);
                g.DrawLine(pen, p.X + 10.4f * s, p.Y + 7.5f * s, p.X + 9.9f * s, p.Y + 13.5f * s);
                break;

            case VectorIcon.Info:
                g.DrawEllipse(pen, p.X + 0.8f * s, p.Y + 0.8f * s, 16.4f * s, 16.4f * s);
                g.FillEllipse(brush, p.X + 7.9f * s, p.Y + 4.4f * s, 2.2f * s, 2.2f * s);
                g.DrawLine(pen, p.X + 9 * s, p.Y + 8.4f * s, p.X + 9 * s, p.Y + 13.4f * s);
                break;

            case VectorIcon.Power:
                g.DrawArc(pen, p.X + 1 * s, p.Y + 1 * s, 14 * s, 14 * s, -60, 300);
                g.DrawLine(pen, p.X + 8 * s, p.Y - 1 * s, p.X + 8 * s, p.Y + 8 * s);
                break;

            case VectorIcon.Sun:
                g.DrawEllipse(pen, p.X + 5 * s, p.Y + 5 * s, 8 * s, 8 * s);
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4;
                    g.DrawLine(pen,
                        p.X + 9 * s + (float)(Math.Cos(angle) * 7 * s), p.Y + 9 * s + (float)(Math.Sin(angle) * 7 * s),
                        p.X + 9 * s + (float)(Math.Cos(angle) * 9 * s), p.Y + 9 * s + (float)(Math.Sin(angle) * 9 * s));
                }
                break;

            case VectorIcon.Moon:
                using (var path = new GraphicsPath())
                {
                    path.AddArc(p.X + 2 * s, p.Y + 1 * s, 14 * s, 16 * s, 60, 240);
                    path.AddArc(p.X + 7 * s, p.Y + 1 * s, 12 * s, 16 * s, 280, -190);
                    path.CloseFigure();
                    g.FillPath(brush, path);
                }
                break;

            case VectorIcon.Warning:
                using (var path = new GraphicsPath())
                {
                    path.AddPolygon([
                        new PointF(p.X + 9 * s, p.Y + 1 * s),
                        new PointF(p.X + 17.5f * s, p.Y + 16 * s),
                        new PointF(p.X + 0.5f * s, p.Y + 16 * s)
                    ]);
                    g.DrawPath(pen, path);
                }
                g.DrawLine(pen, p.X + 9 * s, p.Y + 6 * s, p.X + 9 * s, p.Y + 11 * s);
                g.FillEllipse(brush, p.X + 8 * s, p.Y + 13 * s, 2 * s, 2 * s);
                break;

            case VectorIcon.Check:
                using (var pen2 = new Pen(color, 1.8f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLines(pen2, [
                        new PointF(p.X + 3 * s, p.Y + 9 * s),
                        new PointF(p.X + 7 * s, p.Y + 13 * s),
                        new PointF(p.X + 15 * s, p.Y + 4 * s)
                    ]);
                }
                break;
        }
    }
}

internal sealed class PageHeader : Control
{
    public PageHeader(string title, string subtitle)
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Title = title;
        Subtitle = subtitle;
        Height = 62;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Subtitle { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);
        TextRenderer.DrawText(e.Graphics, Title, UiTheme.Section,
            new Rectangle(0, 4, Width, 26), UiTheme.Text,
            TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, Subtitle, UiTheme.CaptionText,
            new Rectangle(0, 32, Width, 20), UiTheme.TextTertiary,
            TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}

internal enum WorkResult
{
    None,
    Success,
    Failure
}

internal sealed class NavItem : Control
{
    private readonly System.Windows.Forms.Timer _spinTimer = new() { Interval = 40 };
    private readonly System.Windows.Forms.Timer _flashTimer = new() { Interval = 2600 };

    private bool _hover;
    private bool _selected;
    private bool _busy;
    private float _spin;
    private WorkResult _result;

    public NavItem(VectorIcon icon, string label)
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Icon = icon;
        Label = label;
        Size = new Size(212, 44);
        Cursor = Cursors.Hand;

        _spinTimer.Tick += (_, _) => { _spin += 0.34f; Invalidate(); };
        _flashTimer.Tick += (_, _) => { _flashTimer.Stop(); _result = WorkResult.None; Invalidate(); };
        Disposed += (_, _) => { _spinTimer.Dispose(); _flashTimer.Dispose(); };
    }

    public VectorIcon Icon { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Label { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;
            _selected = value;
            Invalidate();
        }
    }

    public void BeginWork()
    {
        if (_busy || IsDisposed)
            return;

        _busy = true;
        _hover = false;
        _result = WorkResult.None;
        _flashTimer.Stop();
        Enabled = false;
        _spinTimer.Start();
        Invalidate();
    }

    public void EndWork(bool success)
    {
        if (!_busy || IsDisposed)
            return;

        _busy = false;
        _spinTimer.Stop();
        Enabled = true;
        _result = success ? WorkResult.Success : WorkResult.Failure;
        _flashTimer.Stop();
        _flashTimer.Start();
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);

        var pill = new Rectangle(1, 2, Width - 2, Height - 4);
        int radius = pill.Height / 2;

        if (_selected && !_busy)
        {
            using (var halo = new SolidBrush(UiTheme.Alpha(UiTheme.Accent, 40)))
                e.Graphics.FillPath(halo, UiTheme.RoundedRect(new Rectangle(0, 1, Width, Height - 2), pill.Height / 2));

            using var path = UiTheme.RoundedRect(pill, radius);
            using var fill = new LinearGradientBrush(pill, UiTheme.PillA, UiTheme.PillB, 25f);
            using var pen = new Pen(UiTheme.Alpha(Color.White, 95), 1.1f);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }
        else if (_hover && !_busy)
        {
            using var path = UiTheme.RoundedRect(pill, radius);
            using var fill = new SolidBrush(UiTheme.GlassFill);
            e.Graphics.FillPath(fill, path);
        }

        Color iconColor = _selected ? Color.White : UiTheme.TextTertiary;
        Color textColor = _busy
            ? UiTheme.TextTertiary
            : _selected ? Color.White : UiTheme.TextSecondary;

        VectorIcons.Draw(e.Graphics, Icon, new Point(22, Height / 2 - 9), iconColor);

        TextRenderer.DrawText(e.Graphics, Label, _selected && !_busy ? UiTheme.NavStrong : UiTheme.Nav,
            new Rectangle(54, 0, Width - 82, Height), textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        DrawActivity(e.Graphics);
    }

    private void DrawActivity(Graphics g)
    {
        int x = Width - 28;
        int y = Height / 2 - 9;

        if (_busy)
        {
            using var pen = new Pen(UiTheme.Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(pen, x, y, 18, 18, _spin * 57.29578f, 250);
            return;
        }

        switch (_result)
        {
            case WorkResult.Success:
                VectorIcons.Draw(g, VectorIcon.Check, new Point(x, y), UiTheme.Success, 18f);
                break;
            case WorkResult.Failure:
                VectorIcons.Draw(g, VectorIcon.Warning, new Point(x, y), UiTheme.Critical, 18f);
                break;
        }
    }
}

internal sealed class ThemeSwitch : Control
{
    private bool _hover;

    public ThemeSwitch()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Height = 42;
        Cursor = Cursors.Hand;
    }

    public event EventHandler? Toggled;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked { get; set; }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
        {
            Checked = !Checked;
            Invalidate();
            Toggled?.Invoke(this, EventArgs.Empty);
        }

        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);

        var pill = new Rectangle(1, 2, Width - 2, Height - 4);

        if (_hover)
        {
            using var path = UiTheme.RoundedRect(pill, pill.Height / 2);
            using var fill = new SolidBrush(UiTheme.GlassFill);
            e.Graphics.FillPath(fill, path);
        }

        VectorIcons.Draw(e.Graphics, Checked ? VectorIcon.Sun : VectorIcon.Moon,
            new Point(22, Height / 2 - 9), UiTheme.TextTertiary);

        TextRenderer.DrawText(e.Graphics, "Light theme", UiTheme.Nav,
            new Rectangle(54, 0, Width - 110, Height), UiTheme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        var track = new Rectangle(Width - 56, Height / 2 - 10, 38, 20);
        using (var path = UiTheme.RoundedRect(track, 10))
        {
            using var fill = new SolidBrush(Checked ? UiTheme.AccentFill : UiTheme.StrokeStrong);
            e.Graphics.FillPath(fill, path);
        }

        int knobX = Checked ? track.Right - 18 : track.X + 2;
        using (var knob = new SolidBrush(Color.White))
            e.Graphics.FillEllipse(knob, knobX, track.Y + 2, 16, 16);
    }
}

internal sealed class SidebarStatusCard : Control
{
    private StatusVisual _visual;

    public SidebarStatusCard()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Height = 84;
    }

    public void SetStatus(StatusVisual visual)
    {
        _visual = visual;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        using (var path = UiTheme.RoundedRect(bounds, 20))
        {
            using var fill = new SolidBrush(UiTheme.GlassFill);
            using var pen = new Pen(UiTheme.GlassBorder);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }

        using (var dot = new SolidBrush(_visual.Accent))
            e.Graphics.FillEllipse(dot, 16, 17, 9, 9);

        TextRenderer.DrawText(e.Graphics, _visual.SidebarLine1, UiTheme.BodyStrong,
            new Rectangle(34, 12, Width - 46, 20), _visual.Accent, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, _visual.Subtitle, UiTheme.CaptionText,
            new Rectangle(16, 38, Width - 32, 16), UiTheme.TextSecondary,
            TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, _visual.SidebarLine2, UiTheme.CaptionText,
            new Rectangle(16, 56, Width - 32, 16), UiTheme.TextTertiary,
            TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class HeroToggle : Control
{
    private readonly System.Windows.Forms.Timer _pulseTimer;
    private StatusVisual _visual;
    private bool _hover;
    private bool _pressed;
    private bool _pulsing;
    private float _phase;

    public HeroToggle()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Size = new Size(440, 296);
        Cursor = Cursors.Hand;

        _pulseTimer = new System.Windows.Forms.Timer { Interval = 40 };
        _pulseTimer.Tick += (_, _) => { _phase += 0.13f; Invalidate(); };
        Disposed += (_, _) => _pulseTimer.Dispose();
    }

    public event EventHandler? Toggled;

    public void SetVisual(StatusVisual visual, bool pulsing)
    {
        _visual = visual;
        if (_pulsing != pulsing)
        {
            _pulsing = pulsing;
            _pulseTimer.Enabled = pulsing;
            if (!pulsing)
                _phase = 0;
        }

        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        bool wasPressed = _pressed;
        _pressed = false;
        Invalidate();

        if (wasPressed && e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
            Toggled?.Invoke(this, EventArgs.Empty);

        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);

        var card = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = UiTheme.RoundedRect(card, 30))
        {
            using var fill = new SolidBrush(UiTheme.GlassFill);
            using var pen = new Pen(UiTheme.GlassBorder);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }

        int cx = Width / 2;
        const int radius = 86;
        int cy = Math.Max(radius + 26, Height / 2 - 34);

        double wave = _pulsing ? (Math.Sin(_phase) + 1) / 2 : 0;

        for (int i = 0; i < 4; i++)
        {
            int rr = radius + 20 + i * 14 + (int)Math.Round(wave * 6);
            int alpha = (int)(30 - i * 6 + wave * 16 + (_hover ? 10 : 0));
            using var glow = new Pen(UiTheme.Alpha(_visual.Accent, Math.Clamp(alpha, 4, 90)), 10f);
            e.Graphics.DrawEllipse(glow, cx - rr, cy - rr, rr * 2, rr * 2);
        }

        using (var disc = new GraphicsPath())
        {
            disc.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
            Color discColor = _pressed ? UiTheme.Mix(_visual.Disc, Color.Black, 0.16) : _visual.Disc;
            using var glass = new LinearGradientBrush(
                new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2),
                UiTheme.Mix(discColor, Color.White, UiTheme.IsDark ? 0.10 : 0.18), discColor, 70f);
            e.Graphics.FillPath(glass, disc);
        }

        using (var ring = new Pen(UiTheme.Alpha(Color.White, _hover ? 175 : 120), 1.6f))
            e.Graphics.DrawEllipse(ring, cx - radius, cy - radius, radius * 2, radius * 2);

        DrawPowerGlyph(e.Graphics, new PointF(cx, cy), radius - 9, _visual.Glyph);

        TextRenderer.DrawText(e.Graphics, _visual.Headline, UiTheme.Hero,
            new Rectangle(0, cy + radius + 24, Width, 34), _visual.Accent,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics, _visual.Subtitle, UiTheme.Body,
            new Rectangle(0, cy + radius + 60, Width, 22), UiTheme.TextSecondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }


    private static void DrawPowerGlyph(Graphics g, PointF c, float r, Color color)
    {
        using var pen = new Pen(color, r * 0.15f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float rr = r * 0.56f;
        g.DrawArc(pen, c.X - rr, c.Y - rr * 0.94f, rr * 2, rr * 2, -64, 308);
        g.DrawLine(pen, c.X, c.Y - rr - r * 0.10f, c.X, c.Y - rr * 0.12f);
    }
}

internal enum NoticeKind
{
    Warning,
    Success,
    Failure
}

internal sealed class NoticeBar : Control
{
    private NoticeKind _kind = NoticeKind.Warning;

    public NoticeBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Height = 46;
        Visible = false;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Message { get; set; } = string.Empty;

    public void Show(string message, NoticeKind kind = NoticeKind.Warning)
    {
        Message = message;
        _kind = kind;
        Visible = true;
        Invalidate();
    }

    public new void Hide()
    {
        Visible = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        Color accent = _kind switch
        {
            NoticeKind.Success => UiTheme.Success,
            NoticeKind.Failure => UiTheme.Critical,
            _ => UiTheme.Caution
        };

        using (var path = UiTheme.RoundedRect(bounds, 8))
        {
            using var fill = new SolidBrush(UiTheme.Mix(UiTheme.Layer, accent, 0.16));
            using var pen = new Pen(UiTheme.Alpha(accent, 140));
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }

        VectorIcons.Draw(
            e.Graphics,
            _kind == NoticeKind.Success ? VectorIcon.Check : _kind == NoticeKind.Failure ? VectorIcon.Warning : VectorIcon.Warning,
            new Point(16, Height / 2 - 9),
            accent);

        TextRenderer.DrawText(e.Graphics, Message, UiTheme.Body,
            new Rectangle(46, 0, Width - 62, Height), UiTheme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}

internal static class AppIcon
{
    private static Icon? _value;
    private static bool _loaded;

    public static Icon? Value
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
                _value = Load();
            }

            return _value;
        }
    }

    private static Icon? Load()
    {
        try
        {
            using Stream? stream = typeof(AppIcon).Assembly
                .GetManifestResourceStream("VpnKillSwitch.Assets.app.ico");
            if (stream is not null)
                return new Icon(stream, new Size(32, 32));
        }
        catch (Exception)
        {
        }

        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

internal abstract class PagePanel : Panel
{
    protected PagePanel()
    {
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        Padding = new Padding(32, 20, 32, 24);
        BackColor = Color.Transparent;
    }

    protected static FluentButton Button(string text, FluentButtonStyle kind, int width, EventHandler action)
    {
        var button = new FluentButton
        {
            Text = text,
            Kind = kind,
            Width = width,
            Height = 34,
            Margin = new Padding(8, 0, 0, 0)
        };
        button.Click += action;
        return button;
    }
}
