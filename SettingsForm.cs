using System.ComponentModel;

namespace VpnKillSwitch;

public sealed class SettingsForm : Form
{
    private readonly GuardController _guard;

    private readonly Panel _caption = new();
    private readonly Panel _sidebar = new();
    private readonly Panel _pageHost = new();
    private readonly SidebarStatusCard _statusCard = new();
    private readonly ThemeSwitch _themeSwitch = new();
    private readonly CaptionButton _maxButton = new();
    private readonly NavItem _runCheck = new(VectorIcon.Play, "Check now");
    private readonly NavItem _clearRules = new(VectorIcon.Trash, "Clear firewall rules");

    private readonly NavItem[] _navItems = new NavItem[5];
    private readonly PagePanel[] _pages = new PagePanel[6];

    private readonly DashboardPage _dashboard;
    private readonly DiagnosticsPage _diagnostics;
    private readonly TunnelPage _tunnel;
    private bool _alert;

    public SettingsForm(GuardController guard)
    {
        _guard = guard;

        Text = "VPN Kill Switch";
        Icon = AppIcon.Value;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(1000, 660);
        Size = new Size(1200, 780);
        Font = UiTheme.Body;
        ForeColor = UiTheme.Text;
        DoubleBuffered = true;

        _dashboard = new DashboardPage();
        _diagnostics = new DiagnosticsPage(guard);
        _tunnel = new TunnelPage(guard.Settings);

        _pages[(int)AppPage.Dashboard] = _dashboard;
        _pages[(int)AppPage.Applications] = new ApplicationsPage(guard.Settings);
        _pages[(int)AppPage.Tunnel] = _tunnel;
        _pages[(int)AppPage.Sensitivity] = new SensitivityPage(guard.Settings);
        _pages[(int)AppPage.Diagnostics] = _diagnostics;
        _pages[(int)AppPage.Info] = new InfoPage();

        _dashboard.ToggleRequested += OnToggleRequested;

        _pageHost.Dock = DockStyle.Fill;
        _pageHost.BackColor = Color.Transparent;
        foreach (PagePanel page in _pages)
            _pageHost.Controls.Add(page);

        BuildSidebar();
        BuildCaption();

        Controls.Add(_pageHost);
        Controls.Add(_sidebar);
        Controls.Add(_caption);

        ShowPage(AppPage.Dashboard);

        _guard.StatusChanged += OnStatusChanged;
        HandleCreated += (_, _) => ApplyChrome();
        Resize += (_, _) => ApplyChrome();
        Shown += (_, _) => UiTheme.ApplyNativeThemes(this);
        UiTheme.Changed += OnThemeChanged;
        FormClosed += OnFormClosed;

        ApplyChrome();
        UpdateStatus(_guard.GetStatus());
    }

    public event EventHandler? ExitRequested;

    internal void ShowPage(AppPage page)
    {
        int index = (int)page;

        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].Visible = i == index;
            if (i == index)
                _pages[i].BringToFront();
        }

        for (int i = 0; i < _navItems.Length; i++)
            _navItems[i].Selected = i == index;

        if (page == AppPage.Diagnostics)
            _diagnostics.SetStatus(_guard.GetStatus());
    }

    internal void ShowWarning(AppPage page, string message)
    {
        ShowPage(page);
        if (_pages[(int)page] is TunnelPage tp)
            tp.ShowNotice(message);
        else if (_pages[(int)page] is ApplicationsPage ap)
            ap.ShowNotice(message);
    }

    internal void ShowWarning(string message) => ShowWarning(AppPage.Tunnel, message);

    protected override void WndProc(ref Message m)
    {
        const int wmNcHitTest = 0x84;
        const int htClient = 1;
        const int htLeft = 10;
        const int htRight = 11;
        const int htTop = 12;
        const int htTopLeft = 13;
        const int htTopRight = 14;
        const int htBottom = 15;
        const int htBottomLeft = 16;
        const int htBottomRight = 17;

        if (m.Msg == wmNcHitTest && WindowState != FormWindowState.Maximized)
        {
            base.WndProc(ref m);
            if ((int)m.Result == htClient)
            {
                int packed = unchecked((int)m.LParam.ToInt64());
                Point p = PointToClient(new Point((short)packed, (short)(packed >> 16)));
                const int grip = 7;
                bool left = p.X <= grip;
                bool right = p.X >= Width - grip;
                bool top = p.Y <= grip;
                bool bottom = p.Y >= Height - grip;

                if (top && left) m.Result = htTopLeft;
                else if (top && right) m.Result = htTopRight;
                else if (bottom && left) m.Result = htBottomLeft;
                else if (bottom && right) m.Result = htBottomRight;
                else if (left) m.Result = htLeft;
                else if (right) m.Result = htRight;
                else if (top) m.Result = htTop;
                else if (bottom) m.Result = htBottom;
            }

            return;
        }

        base.WndProc(ref m);
    }

    private void BuildCaption()
    {
        _caption.Dock = DockStyle.Top;
        _caption.Height = 48;
        _caption.BackColor = Color.Transparent;
        _caption.MouseDown += DragWindow;

        var mark = new CaptionMark { Size = new Size(28, 28), Location = new Point(18, 10) };
        mark.MouseDown += DragWindow;

        var title = new Label
        {
            Text = "VPN Kill Switch",
            ForeColor = UiTheme.Text,
            Font = UiTheme.CaptionTitle,
            Location = new Point(56, 4),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        var subtitle = new Label
        {
            Text = "Protect your applications and real IP when the VPN tunnel drops",
            ForeColor = UiTheme.TextTertiary,
            Font = UiTheme.CaptionText,
            Location = new Point(58, 26),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        title.MouseDown += DragWindow;
        subtitle.MouseDown += DragWindow;

        var close = new CaptionButton { Glyph = Glyph("\uE8BB", "\u00D7"), IsClose = true, Dock = DockStyle.Right };
        close.Click += (_, _) => Close();

        _maxButton.Glyph = Glyph("\uE922", "\u25A1");
        _maxButton.Dock = DockStyle.Right;
        _maxButton.Click += (_, _) => ToggleMax();

        var minimise = new CaptionButton { Glyph = Glyph("\uE921", "\u2013"), Dock = DockStyle.Right };
        minimise.Click += (_, _) => WindowState = FormWindowState.Minimized;

        var help = new CaptionButton { Glyph = Glyph("\uE897", "?"), Dock = DockStyle.Right };
        help.Click += (_, _) => ShowPage(AppPage.Info);
        var helpTip = new ToolTip();
        helpTip.SetToolTip(help, "Guide & Help");

        _caption.Controls.AddRange(mark, title, subtitle);
        _caption.Controls.Add(help);
        _caption.Controls.Add(minimise);
        _caption.Controls.Add(_maxButton);
        _caption.Controls.Add(close);

        UiTheme.Bind(title, () => title.ForeColor = UiTheme.Text);
        UiTheme.Bind(subtitle, () => subtitle.ForeColor = UiTheme.TextTertiary);
    }

    private void BuildSidebar()
    {
        _sidebar.Dock = DockStyle.Left;
        _sidebar.Width = 236;
        _sidebar.BackColor = Color.Transparent;
        _sidebar.Padding = new Padding(12, 16, 12, 16);

        var navStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 230,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        (VectorIcon Icon, string Label, AppPage Page)[] entries =
        [
            (VectorIcon.Dashboard, "Dashboard", AppPage.Dashboard),
            (VectorIcon.Applications, "Applications", AppPage.Applications),
            (VectorIcon.Endpoints, "Tunnel", AppPage.Tunnel),
            (VectorIcon.Sensitivity, "Sensitivity", AppPage.Sensitivity),
            (VectorIcon.Activity, "Diagnostics", AppPage.Diagnostics)
        ];

        for (int i = 0; i < entries.Length; i++)
        {
            AppPage target = entries[i].Page;
            var item = new NavItem(entries[i].Icon, entries[i].Label)
            {
                Width = 212,
                Margin = new Padding(0, 0, 0, 4)
            };
            item.Click += (_, _) => ShowPage(target);
            _navItems[i] = item;
            navStack.Controls.Add(item);
        }

        var bottomStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 296,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        _statusCard.Width = 212;
        _statusCard.Margin = new Padding(0, 0, 0, 10);

        var rule = new Panel { Width = 212, Height = 1, Margin = new Padding(0, 0, 0, 14) };
        UiTheme.Bind(rule, () => rule.BackColor = UiTheme.Stroke);

        _themeSwitch.Width = 212;
        _themeSwitch.Checked = UiTheme.Current == AppTheme.Light;
        _themeSwitch.Toggled += (_, _) => SwitchTheme();

        bottomStack.Controls.Add(_statusCard);
        bottomStack.Controls.Add(rule);
        bottomStack.Controls.Add(_themeSwitch);
        bottomStack.Controls.Add(_runCheck);
        bottomStack.Controls.Add(_clearRules);
        bottomStack.Controls.Add(RailAction(VectorIcon.Power, "Exit", OnExitRequested));

        _runCheck.Click += OnCheckNow;
        _clearRules.Click += OnCleanupRules;

        _sidebar.Controls.Add(navStack);
        _sidebar.Controls.Add(bottomStack);
    }

    private static NavItem RailAction(VectorIcon icon, string label, EventHandler action)
    {
        var item = new NavItem(icon, label) { Width = 212, Margin = new Padding(0, 4, 0, 0) };
        item.Click += action;
        return item;
    }

    private void ApplyChrome()
    {
        ForeColor = UiTheme.Text;
        WindowChrome.Apply(this, WindowState != FormWindowState.Maximized);
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Rectangle r = ClientRectangle;
        if (r.Width <= 0 || r.Height <= 0)
            return;

        e.Graphics.DrawImageUnscaled(
            GlassBackground.Get(r.Width, r.Height, _alert, _sidebar.Width, _caption.Height), 0, 0);
    }

    private void OnThemeChanged()
    {
        ApplyChrome();

        if (IsHandleCreated)
            UiTheme.ApplyNativeThemes(this);
    }

    private void SwitchTheme()
    {
        AppTheme theme = _themeSwitch.Checked ? AppTheme.Light : AppTheme.Dark;
        if (theme == UiTheme.Current)
            return;

        UiTheme.Apply(theme);
        _guard.Settings.Theme = theme;
        _guard.Settings.Save();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        _guard.StatusChanged -= OnStatusChanged;
        UiTheme.Changed -= OnThemeChanged;
    }

    private void OnStatusChanged(GuardStatus status)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateStatus(status));
            return;
        }

        UpdateStatus(status);
    }

    private void UpdateStatus(GuardStatus status)
    {
        _statusCard.SetStatus(UiTheme.ForGuard(status));
        _dashboard.UpdateStatus(status);
        _diagnostics.SetStatus(status);

        bool alert = status.Armed && status.Verdict == GuardVerdict.Leaking;
        if (alert != _alert)
        {
            _alert = alert;
            Invalidate();
        }
    }

    private async void OnToggleRequested(object? sender, EventArgs e)
    {
        try
        {
            if (_guard.IsArmed)
            {
                await _guard.DisarmAsync();
                return;
            }

            if (!_guard.Settings.ProtectedApps.Any(app => app.Enabled))
            {
                ShowWarning(AppPage.Applications, "Add at least one application before you start protection.");
                return;
            }

            if (!_guard.Settings.HasAnySignal)
            {
                ShowWarning(AppPage.Tunnel, "No tunnel signal is configured. Use Detect automatically, or fill the fields in by hand.");
                return;
            }

            await _guard.ArmAsync();
        }
        catch (Exception ex)
        {
            Report(ex);
        }
    }

    private async void OnCheckNow(object? sender, EventArgs e)
    {
        _runCheck.BeginWork();
        try
        {
            await _guard.EvaluateAsync(CancellationToken.None, forceFreshEgress: true);
            _runCheck.EndWork(true);
        }
        catch (Exception ex)
        {
            _runCheck.EndWork(false);
            Report(ex);
        }
    }

    private async void OnCleanupRules(object? sender, EventArgs e)
    {
        _clearRules.BeginWork();
        try
        {
            FirewallResult result = await _guard.ClearRulesAsync();
            _clearRules.EndWork(result.Success);

            if (!result.Success)
                Report(new InvalidOperationException(result.Detail));
        }
        catch (Exception ex)
        {
            _clearRules.EndWork(false);
            Report(ex);
        }
    }

    private void OnExitRequested(object? sender, EventArgs e) => ExitRequested?.Invoke(this, EventArgs.Empty);

    private void Report(Exception ex)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            MessageBox.Show(ex.Message, "VPN Kill Switch", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show(this, ex.Message, "VPN Kill Switch", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void DragWindow(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || WindowState == FormWindowState.Maximized)
            return;

        NativeInput.ReleaseCapture();
        NativeInput.SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
    }

    private void ToggleMax()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Normal;
            _maxButton.Glyph = Glyph("\uE922", "\u25A1");
        }
        else
        {
            WindowState = FormWindowState.Maximized;
            _maxButton.Glyph = Glyph("\uE923", "\u29C8");
        }
    }

    private static string Glyph(string fluent, string fallback)
    {
        string family = UiTheme.Glyph.FontFamily.Name;
        return family.Contains("Icon", StringComparison.OrdinalIgnoreCase) || family.Contains("MDL2")
            ? fluent
            : fallback;
    }

    private sealed class CaptionMark : Control
    {
        public CaptionMark()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiTheme.ConfigureGraphics(e.Graphics);
            using var fill = new SolidBrush(UiTheme.AccentFill);
            using var path = UiTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 7);
            e.Graphics.FillPath(fill, path);

            using var shield = new SolidBrush(Color.White);
            e.Graphics.FillPolygon(shield, [
                new Point(14, 7),
                new Point(21, 10),
                new Point(21, 16),
                new Point(14, 22),
                new Point(7, 16),
                new Point(7, 10)
            ]);
        }
    }
}
