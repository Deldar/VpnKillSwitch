namespace VpnKillSwitch;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly GuardController _guard;
    private readonly TrayIconSet _icons;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly Control _uiDispatcher = new();
    private SettingsForm? _settingsForm;

    private TrayVisualState _lastVisualState = TrayVisualState.Idle;

    public TrayApplicationContext(GuardController guard)
    {
        _guard = guard;
        _icons = new TrayIconSet();
        _uiDispatcher.CreateControl();
        _ = _uiDispatcher.Handle;
        _guard.StatusChanged += OnStatusChanged;

        _toggleItem = new ToolStripMenuItem("Start protection", null, OnToggle);

        _menu = new ContextMenuStrip
        {
            Renderer = new FluentMenuRenderer(),
            Font = UiTheme.Body,
            ShowImageMargin = false,
            Padding = new Padding(6, 8, 6, 8)
        };
        _menu.Items.Add(_toggleItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Settings", null, (_, _) => OpenSettings(AppPage.Dashboard, null)));
        _menu.Items.Add(new ToolStripMenuItem("Clear firewall rules", null, OnCleanupRules));
        _menu.Items.Add(new ToolStripMenuItem("Exit", null, OnExit));

        _trayIcon = new NotifyIcon
        {
            Icon = _icons.GetIcon(TrayVisualState.Idle),
            Text = "VPN Kill Switch: idle",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _trayIcon.DoubleClick += (_, _) => OpenSettings(AppPage.Dashboard, null);

        UiTheme.Changed += ApplyMenuTheme;
        ApplyMenuTheme();

        UpdateTray(_guard.GetStatus());
    }

    private void ApplyMenuTheme()
    {
        _menu.BackColor = UiTheme.Layer;
        _menu.ForeColor = UiTheme.Text;
        _menu.Renderer = new FluentMenuRenderer();
    }

    private async void OnToggle(object? sender, EventArgs e)
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
                OpenSettings(AppPage.Applications, "Add at least one application before you start protection.");
                return;
            }

            if (!_guard.Settings.HasAnySignal)
            {
                OpenSettings(AppPage.Tunnel, "No tunnel signal is configured. Use Detect automatically, or fill the fields in by hand.");
                return;
            }

            await _guard.ArmAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "VPN Kill Switch", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateTray(_guard.GetStatus());
        }
    }

    private async void OnCleanupRules(object? sender, EventArgs e)
    {
        try
        {
            FirewallResult result = await _guard.ClearRulesAsync();
            if (!result.Success)
                MessageBox.Show(result.Detail, "VPN Kill Switch", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "VPN Kill Switch", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenSettings(AppPage page, string? warning)
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.ShowPage(page);
            if (!string.IsNullOrWhiteSpace(warning))
                _settingsForm.ShowWarning(page, warning);
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_guard);
        _settingsForm.ExitRequested += (_, _) => OnExit(null, EventArgs.Empty);
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();

        if (page != AppPage.Dashboard)
            _settingsForm.ShowPage(page);

        if (!string.IsNullOrWhiteSpace(warning))
            _settingsForm.ShowWarning(page, warning);
    }

    private void OnStatusChanged(GuardStatus status)
    {
        if (_uiDispatcher.IsDisposed)
            return;

        if (_uiDispatcher.InvokeRequired)
        {
            _uiDispatcher.BeginInvoke(() => UpdateTray(status));
            return;
        }

        UpdateTray(status);
    }

    private void UpdateTray(GuardStatus status)
    {
        StatusVisual visual = UiTheme.ForGuard(status);

        _toggleItem.Text = status.Armed ? "Stop protection" : "Start protection";
        _toggleItem.Checked = status.Armed;

        _trayIcon.Icon = _icons.GetIcon(visual.Tray);
        _trayIcon.Text = visual.Tray switch
        {
            TrayVisualState.Connecting => "VPN Kill Switch: checking the tunnel",
            TrayVisualState.Connected => "VPN Kill Switch: traffic is going through the tunnel",
            TrayVisualState.Blocked => $"VPN Kill Switch: leaking - {(status.BlockedCount == 1 ? "1 app" : $"{status.BlockedCount} apps")} blocked",
            _ => "VPN Kill Switch: idle"
        };

        if (visual.Tray != _lastVisualState && status.Armed)
            ShowStateBalloon(visual.Tray, status);

        _lastVisualState = visual.Tray;
    }

    private void ShowStateBalloon(TrayVisualState visual, GuardStatus status)
    {
        NotificationStyle style = _guard.Settings.NotificationStyle;
        if (style == NotificationStyle.Silent)
            return;

        if (style is NotificationStyle.SoundAlert or NotificationStyle.Both)
        {
            if (visual == TrayVisualState.Blocked)
                AudioAlerts.PlayBlocked();
            else if (visual == TrayVisualState.Connected)
                AudioAlerts.PlayRestored();
        }

        if (style is NotificationStyle.WindowsNotification or NotificationStyle.Both)
        {
            var (title, message, icon) = visual switch
            {
                TrayVisualState.Connecting => (
                    "Tunnel Not Confirmed",
                    "Apps stay blocked until the tunnel is proven healthy.",
                    ToolTipIcon.Info),
                TrayVisualState.Connected => (
                    "Protected",
                    "Traffic is going through the tunnel. Apps are allowed online.",
                    ToolTipIcon.Info),
                TrayVisualState.Blocked => (
                    "Real IP Would Leak",
                    $"{(status.BlockedCount == 1 ? "1 protected application is" : $"{status.BlockedCount} protected applications are")} blocked because traffic was routing outside the tunnel.",
                    ToolTipIcon.Warning),
                _ => ("Protection Off", "Nothing is being watched.", ToolTipIcon.None)
            };

            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = message;
            _trayIcon.BalloonTipIcon = icon;
            _trayIcon.ShowBalloonTip(4000);
        }
    }

    private async void OnExit(object? sender, EventArgs e)
    {
        UiTheme.Changed -= ApplyMenuTheme;

        try
        {
            await _guard.DisarmAsync();
        }
        catch (Exception)
        {
        }

        _guard.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _icons.Dispose();
        _uiDispatcher.Dispose();
        ExitThread();
    }
}
