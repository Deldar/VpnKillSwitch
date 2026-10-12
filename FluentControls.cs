using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace VpnKillSwitch;

internal class RoundedSurface : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 20;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? Fill { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? BorderColor { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DrawBorder { get; set; } = true;

    public RoundedSurface()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);
        using var path = UiTheme.RoundedRect(ClientRectangle, Radius);
        using var fill = new SolidBrush(Fill ?? UiTheme.GlassFill);
        e.Graphics.FillPath(fill, path);

        if (DrawBorder)
        {
            using var pen = new Pen(BorderColor ?? UiTheme.GlassBorder);
            e.Graphics.DrawPath(pen, path);
        }
    }
}

internal enum FluentButtonStyle
{
    Accent,
    Subtle,
    Critical
}

internal sealed class FluentButton : Control
{
    private bool _hover;
    private bool _pressed;
    private bool _busy;
    private string? _busyText;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FluentButtonStyle Kind { get; set; } = FluentButtonStyle.Subtle;

    public FluentButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        Height = 32;
        Cursor = Cursors.Hand;
        Font = UiTheme.BodyStrong;
        ForeColor = UiTheme.Text;
        BackColor = Color.Transparent;
        Padding = new Padding(14, 0, 14, 0);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left && !_busy) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }

    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    public void SetBusy(bool busy, string? text = null)
    {
        _busy = busy;
        if (busy && text is not null)
            _busyText = text;

        Enabled = !busy;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);
        Color fill = Kind switch
        {
            FluentButtonStyle.Accent => _pressed ? UiTheme.AccentFillPressed : _hover ? UiTheme.AccentFillHover : UiTheme.AccentFill,
            FluentButtonStyle.Critical => _pressed
                ? UiTheme.Mix(UiTheme.CriticalFill, Color.Black, 0.25)
                : _hover
                    ? UiTheme.CriticalFill
                    : UiTheme.Mix(UiTheme.Layer, UiTheme.CriticalFill, 0.35),
            _ => UiTheme.Alpha(UiTheme.IsDark ? Color.White : Color.Black, _pressed ? 28 : _hover ? 22 : 14)
        };

        if (_busy)
            fill = UiTheme.Mix(fill, UiTheme.Layer, 0.45);

        Color text = Kind == FluentButtonStyle.Accent ? Color.White : Kind == FluentButtonStyle.Critical ? UiTheme.Critical : UiTheme.Text;
        if (_busy)
            text = UiTheme.TextTertiary;

        using var path = UiTheme.RoundedRect(ClientRectangle, ClientRectangle.Height / 2);
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(Kind == FluentButtonStyle.Subtle ? UiTheme.GlassBorder : UiTheme.Alpha(Color.White, 70));
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);

        TextRenderer.DrawText(
            e.Graphics,
            _busy ? _busyText ?? Text : Text,
            Font,
            ClientRectangle,
            text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}

internal sealed class CaptionButton : Control
{
    private bool _hover;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsClose { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Glyph { get; set; } = "";

    public CaptionButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
            true);
        Size = new Size(46, 52);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);
        if (_hover)
        {
            int d = Math.Max(12, Math.Min(Width, Height) - 14);
            var circle = new Rectangle((Width - d) / 2, (Height - d) / 2, d, d);
            using var fill = new SolidBrush(IsClose ? UiTheme.CriticalFill : UiTheme.GlassFillStrong);
            e.Graphics.FillEllipse(fill, circle);
        }

        Color glyphColor = IsClose && _hover ? Color.White : UiTheme.Text;
        TextRenderer.DrawText(
            e.Graphics,
            Glyph,
            UiTheme.Glyph.FontFamily.Name.Contains("Icon", StringComparison.OrdinalIgnoreCase) || UiTheme.Glyph.FontFamily.Name.Contains("MDL2")
                ? UiTheme.Glyph
                : UiTheme.Body,
            ClientRectangle,
            glyphColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed class ProtectedAppsList : RoundedSurface
{
    private readonly FlowLayoutPanel _rows = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = Color.Transparent,
        Padding = new Padding(8, 4, 8, 8)
    };

    private readonly Label _empty = new()
    {
        Text = "No applications protected yet.\n\nClick 'Detect installed' to automatically discover your IDEs and AI tools, or add applications manually.",
        ForeColor = UiTheme.TextTertiary,
        Font = UiTheme.Body,
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Padding = new Padding(32)
    };

    public ProtectedAppsList()
    {
        Fill = Color.Transparent;
        DrawBorder = false;
        Controls.Add(_rows);
        Controls.Add(_empty);
        _rows.Resize += (_, _) =>
        {
            foreach (Control row in _rows.Controls)
                row.Width = Math.Max(120, _rows.ClientSize.Width - 8);
        };
        _rows.ControlAdded += (_, _) => _rows.HorizontalScroll.Maximum = 0;
        _rows.HandleCreated += (_, _) => UiTheme.ApplyNativeTheme(_rows);
        UiTheme.Bind(_rows, () => UiTheme.ApplyNativeTheme(_rows));
        UiTheme.Bind(_empty, () => _empty.ForeColor = UiTheme.TextTertiary);
        ShowEmptyIfNeeded();
    }

    public event EventHandler? AppChanged;

    public int AppCount => _rows.Controls.OfType<AppRow>().Count();

    public void ClearApps()
    {
        _rows.Controls.Clear();
        ShowEmptyIfNeeded();
    }

    public void AddApp(ProtectedApp app)
    {
        var row = new AppRow(app);
        row.Width = Math.Max(120, _rows.ClientSize.Width - 8);
        row.Margin = new Padding(4, 4, 4, 0);
        row.AppChanged += (_, _) => AppChanged?.Invoke(this, EventArgs.Empty);
        row.SelectedChanged += (_, _) =>
        {
            if (ModifierKeys.HasFlag(Keys.Control))
                return;
            foreach (AppRow other in _rows.Controls.OfType<AppRow>())
            {
                if (other == row)
                    continue;
                other.Selected = false;
                other.Invalidate();
            }
        };
        _rows.Controls.Add(row);
        ShowEmptyIfNeeded();
    }

    public void RemoveSelected()
    {
        foreach (AppRow row in _rows.Controls.OfType<AppRow>().Where(item => item.Selected).ToArray())
            _rows.Controls.Remove(row);
        ShowEmptyIfNeeded();
    }

    public IReadOnlyList<ProtectedApp> GetApps() =>
        _rows.Controls.OfType<AppRow>().Select(row => row.App).ToList();

    private void ShowEmptyIfNeeded()
    {
        bool empty = _rows.Controls.Count == 0;
        _empty.Visible = empty;
        _rows.Visible = !empty;
    }

    private sealed class AppRow : Control
    {
        private static readonly ToolTip RowTip = new()
        {
            InitialDelay = 400,
            ReshowDelay = 150,
            AutoPopDelay = 12000
        };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selected { get; set; }

        public event EventHandler? SelectedChanged;
        public event EventHandler? AppChanged;

        public AppRow(ProtectedApp app)
        {
            App = app;
            Height = 58;
            Cursor = Cursors.Hand;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;

            string status = File.Exists(app.Path) ? "Ready" : "File missing on disk";
            string strategy = app.PathStrategy == AppPathStrategy.VersionAware ? "Auto-update version" : "Exact path";
            RowTip.SetToolTip(this, $"{app.Name}\nPath: {app.Path}\nMode: {strategy}\nStatus: {status}");
        }

        public ProtectedApp App { get; }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.X <= 42)
            {
                App.Enabled = !App.Enabled;
                AppChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
                return;
            }

            Selected = ModifierKeys.HasFlag(Keys.Control) ? !Selected : true;
            SelectedChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiTheme.ConfigureGraphics(e.Graphics);
            Color fill = Selected ? UiTheme.Alpha(UiTheme.AccentFill, 96) : UiTheme.GlassFill;
            using var path = UiTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), (Height - 1) / 2);
            using var brush = new SolidBrush(fill);
            using var pen = new Pen(Selected ? UiTheme.Alpha(UiTheme.Accent, 150) : UiTheme.GlassBorder);
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);

            var check = new Rectangle(14, 20, 18, 18);
            using var checkPath = UiTheme.RoundedRect(check, 4);
            using var checkFill = new SolidBrush(App.Enabled ? UiTheme.AccentFill : UiTheme.Input);
            using var checkPen = new Pen(App.Enabled ? UiTheme.AccentFill : UiTheme.StrokeStrong);
            e.Graphics.FillPath(checkFill, checkPath);
            e.Graphics.DrawPath(checkPen, checkPath);
            if (App.Enabled)
            {
                using var tick = new Pen(Color.White, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                e.Graphics.DrawLines(tick, [new Point(18, 29), new Point(22, 33), new Point(28, 24)]);
            }

            bool exists = File.Exists(App.Path);
            Color nameColor = exists ? UiTheme.Text : UiTheme.Caution;
            Color pathColor = exists ? UiTheme.TextTertiary : UiTheme.Caution;
            string displayName = exists ? App.Name : $"{App.Name} (missing on disk)";

            TextRenderer.DrawText(e.Graphics, displayName, UiTheme.BodyStrong, new Rectangle(42, 8, Width - 190, 24), nameColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics, App.Path, UiTheme.CaptionText, new Rectangle(42, 30, Width - 190, 20), pathColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            string badge = App.PathStrategy == AppPathStrategy.VersionAware ? "Auto-update" : "Exact path";
            var badgeRect = new Rectangle(Width - 118, 18, 102, 22);
            using var badgePath = UiTheme.RoundedRect(badgeRect, 11);
            using var badgeFill = new SolidBrush(UiTheme.GlassFill);
            e.Graphics.FillPath(badgeFill, badgePath);
            TextRenderer.DrawText(e.Graphics, badge, UiTheme.CaptionText, badgeRect, UiTheme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}

internal sealed class FluentMenuRenderer : ToolStripProfessionalRenderer
{
    public FluentMenuRenderer() : base(new FluentColorTable())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? UiTheme.Text : UiTheme.TextTertiary;
        e.TextFont = UiTheme.Body;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled)
            return;

        Rectangle bounds = new(4, 1, e.Item.Width - 8, e.Item.Height - 2);
        using var path = UiTheme.RoundedRect(bounds, 4);
        using var fill = new SolidBrush(UiTheme.Alpha(UiTheme.Text, 24));
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(fill, path);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(UiTheme.Stroke);
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
    }

    private sealed class FluentColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => UiTheme.Stroke;
        public override Color MenuStripGradientBegin => UiTheme.Layer;
        public override Color MenuStripGradientEnd => UiTheme.Layer;
        public override Color ToolStripDropDownBackground => UiTheme.Layer;
        public override Color ImageMarginGradientBegin => UiTheme.Layer;
        public override Color ImageMarginGradientMiddle => UiTheme.Layer;
        public override Color ImageMarginGradientEnd => UiTheme.Layer;
        public override Color SeparatorDark => UiTheme.Stroke;
        public override Color SeparatorLight => UiTheme.Stroke;
    }
}

internal static class NativeInput
{
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
}
