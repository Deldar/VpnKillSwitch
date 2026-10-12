using System.Drawing.Drawing2D;

namespace VpnKillSwitch;

internal static class GlassBackground
{
    private static Bitmap? _cache;
    private static (int W, int H, AppTheme Theme, bool Alert, int Sidebar, int Caption) _key;

    public static Bitmap Get(int width, int height, bool alert, int sidebarWidth, int captionHeight)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        var key = (width, height, UiTheme.Current, alert, sidebarWidth, captionHeight);
        if (_cache is not null && _key == key)
            return _cache;

        _cache?.Dispose();
        _cache = Render(width, height, alert, sidebarWidth, captionHeight);
        _key = key;
        return _cache;
    }

    private static Bitmap Render(int w, int h, bool alert, int sidebarWidth, int captionHeight)
    {
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var full = new Rectangle(0, 0, w, h);
        using (var baseFill = new LinearGradientBrush(full, UiTheme.AuroraBaseA, UiTheme.AuroraBaseB, 40f))
            g.FillRectangle(baseFill, full);

        Color a = alert ? UiTheme.AlertA : UiTheme.AuroraA;
        Color b = alert ? UiTheme.AlertB : UiTheme.AuroraB;
        Color c = alert ? UiTheme.AlertC : UiTheme.AuroraC;

        Blob(g, w, h, 0.18f, 0.12f, 0.62f, a, 210);
        Blob(g, w, h, 0.84f, 0.22f, 0.52f, b, 190);
        Blob(g, w, h, 0.46f, 0.94f, 0.70f, c, 170);

        using (var veil = new SolidBrush(UiTheme.AuroraVeil))
            g.FillRectangle(veil, full);

        using (var sidebar = new SolidBrush(UiTheme.RegionSidebar))
            g.FillRectangle(sidebar, new Rectangle(0, captionHeight, sidebarWidth, h - captionHeight));

        using (var caption = new SolidBrush(UiTheme.RegionCaption))
            g.FillRectangle(caption, new Rectangle(0, 0, w, captionHeight));

        return bmp;
    }

    private static void Blob(Graphics g, int w, int h, float cx, float cy, float radius, Color color, int alpha)
    {
        float r = radius * w;
        var rect = new RectangleF(cx * w - r, cy * h - r, r * 2, r * 2);

        using var path = new GraphicsPath();
        path.AddEllipse(rect);
        using var brush = new PathGradientBrush(path)
        {
            CenterColor = Color.FromArgb(alpha, color),
            SurroundColors = [Color.FromArgb(0, color)]
        };
        g.FillPath(brush, path);
    }
}

internal sealed class StatCard : Control
{
    private string _kicker = string.Empty;
    private string _value = string.Empty;
    private string _detail = string.Empty;
    private Color _accent = Color.Gray;

    public StatCard(string kicker)
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        _kicker = kicker;
    }

    public void Set(string value, string detail, Color accent)
    {
        _value = value;
        _detail = detail;
        _accent = accent;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);

        using (var path = UiTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 20))
        {
            using var fill = new SolidBrush(UiTheme.GlassFill);
            using var pen = new Pen(UiTheme.GlassBorder);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }

        using (var dot = new SolidBrush(_accent))
            e.Graphics.FillEllipse(dot, 22, 22, 10, 10);

        TextRenderer.DrawText(e.Graphics, _kicker.ToUpperInvariant(), UiTheme.CaptionText,
            new Rectangle(42, 18, Width - 58, 18), UiTheme.TextTertiary, TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics, _value, UiTheme.Section,
            new Rectangle(22, 46, Width - 40, 28), _accent, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, _detail, UiTheme.CaptionText,
            new Rectangle(22, 78, Width - 40, 18), UiTheme.TextSecondary,
            TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class FieldCard : Control
{
    private const int FrameHeight = 40;
    private const int StepperBand = 30;

    private readonly TextBox? _text;
    private readonly NumericUpDown? _number;
    private readonly ComboBox? _combo;
    private bool _hoverUp;
    private bool _hoverDown;

    public FieldCard(string title, string hint, TextBox input)
    {
        _text = input;
        Title = title;
        Hint = hint;
        Initialise(input, false);
    }

    public FieldCard(string title, string hint, NumericUpDown number)
    {
        _number = number;
        Title = title;
        Hint = hint;
        Initialise(number, true);
    }

    public FieldCard(string title, string hint, ComboBox combo)
    {
        _combo = combo;
        Title = title;
        Hint = hint;
        Initialise(combo, false);
    }

    public string Title { get; }

    public string Hint { get; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int FrameWidth { get; set; }

    private Rectangle Frame
    {
        get
        {
            int width = FrameWidth > 0
                ? Math.Min(FrameWidth, Math.Max(90, Width - 44))
                : Math.Max(90, Width - 40);
            int x = FrameWidth > 0 ? Width - 22 - width : 20;
            return new Rectangle(x, 74, width, FrameHeight);
        }
    }

    private Rectangle UpArrow => new(Frame.Right - StepperBand, Frame.Y + 4, StepperBand - 6, (FrameHeight - 8) / 2);

    private Rectangle DownArrow => new(Frame.Right - StepperBand, Frame.Y + 4 + (FrameHeight - 8) / 2, StepperBand - 6, (FrameHeight - 8) / 2);

    private void Initialise(Control input, bool numeric)
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;

        if (input is TextBox textBox)
            textBox.BorderStyle = BorderStyle.None;
        else if (input is ComboBox comboBox)
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        input.Font = UiTheme.Body;
        input.BackColor = UiTheme.Input;
        input.ForeColor = UiTheme.Text;
        Controls.Add(input);

        UiTheme.Bind(this, () =>
        {
            input.BackColor = UiTheme.Input;
            input.ForeColor = UiTheme.Text;
            LayoutInput();
            Invalidate();
        });

        Resize += (_, _) => LayoutInput();
        LayoutInput();

        if (!numeric)
            return;

        input.HandleCreated += (_, _) => HideNativeSpinner();
        UiTheme.Bind(input, HideNativeSpinner);
    }

    private void LayoutInput()
    {
        if (Controls.Count == 0)
            return;

        Rectangle frame = Frame;
        int right = _number is null ? 12 : StepperBand + 4;
        Controls[0].Bounds = new Rectangle(
            frame.X + 12,
            frame.Y + (frame.Height - Controls[0].Height) / 2,
            Math.Max(30, frame.Width - 12 - right),
            Controls[0].Height);
    }

    private void HideNativeSpinner()
    {
        if (_number is null)
            return;

        foreach (Control child in _number.Controls)
        {
            if (child is TextBox || !child.Visible)
                continue;

            child.Visible = false;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_number is null)
        {
            base.OnMouseMove(e);
            return;
        }

        bool up = UpArrow.Contains(e.Location);
        bool down = DownArrow.Contains(e.Location);
        if (up != _hoverUp || down != _hoverDown)
        {
            _hoverUp = up;
            _hoverDown = down;
            Cursor = up || down ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverUp = false;
        _hoverDown = false;
        Cursor = Cursors.Default;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (_number is not null && e.Button == MouseButtons.Left)
        {
            if (UpArrow.Contains(e.Location))
            {
                Step(1);
                return;
            }

            if (DownArrow.Contains(e.Location))
            {
                Step(-1);
                return;
            }
        }

        if (e.Button == MouseButtons.Left && Frame.Contains(e.Location) && Controls.Count > 0)
            Controls[0].Focus();

        base.OnMouseDown(e);
    }

    private void Step(int direction)
    {
        if (_number is null)
            return;

        decimal next = _number.Value + _number.Increment * direction;
        _number.Value = Math.Clamp(next, _number.Minimum, _number.Maximum);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);

        using (var path = UiTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 20))
        {
            using var fill = new SolidBrush(UiTheme.GlassFill);
            using var pen = new Pen(UiTheme.GlassBorder);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }

        TextRenderer.DrawText(e.Graphics, Title, UiTheme.BodyStrong,
            new Rectangle(20, 18, Width - 40, 22), UiTheme.Text, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, Hint, UiTheme.CaptionText,
            new Rectangle(20, 42, Width - 40, 18), UiTheme.TextTertiary, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        Rectangle frame = Frame;
        using (var path = UiTheme.RoundedRect(frame, 10))
        {
            using var fill = new SolidBrush(UiTheme.Input);
            using var pen = new Pen(UiTheme.Stroke);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }

        if (_number is null)
            return;

        PaintStep(e.Graphics, UpArrow, true, _hoverUp);
        PaintStep(e.Graphics, DownArrow, false, _hoverDown);
    }

    private static void PaintStep(Graphics g, Rectangle bounds, bool up, bool hovered)
    {
        using var pen = new Pen(hovered ? UiTheme.Accent : UiTheme.TextSecondary, 1.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        int cx = bounds.X + bounds.Width / 2;
        int cy = bounds.Y + bounds.Height / 2;
        const int r = 4;

        Point[] arrow = up
            ?
            [
                new Point(cx - r, cy + 2), new Point(cx, cy - 2), new Point(cx + r, cy + 2)
            ]
            :
            [
                new Point(cx - r, cy - 2), new Point(cx, cy + 2), new Point(cx + r, cy - 2)
            ];

        g.DrawLines(pen, arrow);
    }
}
