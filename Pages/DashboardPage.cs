namespace VpnKillSwitch;

internal sealed class DashboardPage : PagePanel
{
    private readonly HeroToggle _toggle = new();
    private readonly StatCard _tunnel = new("Tunnel");
    private readonly StatCard _egress = new("Egress identity");
    private readonly StatCard _apps = new("Applications");
    private readonly Label _strip = new();
    private readonly System.Windows.Forms.Timer _ticker;

    private GuardStatus? _last;

    public DashboardPage()
    {
        _toggle.Toggled += (_, _) => ToggleRequested?.Invoke(this, EventArgs.Empty);

        var tiles = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 124,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        for (int i = 0; i < 3; i++)
            tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        StatCard[] cards = [_tunnel, _egress, _apps];
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].Dock = DockStyle.Fill;
            cards[i].Margin = new Padding(i == 0 ? 0 : 10, 0, i == cards.Length - 1 ? 0 : 10, 0);
            tiles.Controls.Add(cards[i], i, 0);
        }

        var tilesHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 124 + 20 + 18,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 20, 0, 18)
        };
        tilesHost.Controls.Add(tiles);

        var stripHost = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Color.Transparent };
        var rule = new Panel { Dock = DockStyle.Top, Height = 1 };
        _strip.Dock = DockStyle.Fill;
        _strip.TextAlign = ContentAlignment.MiddleLeft;
        _strip.Font = UiTheme.CaptionText;
        _strip.BackColor = Color.Transparent;
        stripHost.Controls.Add(_strip);
        stripHost.Controls.Add(rule);
        UiTheme.Bind(rule, () => rule.BackColor = UiTheme.Stroke);
        UiTheme.Bind(_strip, UpdateStrip);

        _toggle.Dock = DockStyle.Fill;
        PageLayout.Compose(this, _toggle, [], [tilesHost, stripHost]);

        _ticker = new System.Windows.Forms.Timer { Interval = 1000 };
        _ticker.Tick += (_, _) => UpdateStrip();
        Disposed += (_, _) => _ticker.Dispose();
    }

    public event EventHandler? ToggleRequested;

    public void UpdateStatus(GuardStatus status)
    {
        _last = status;
        _toggle.SetVisual(UiTheme.ForGuard(status), UiTheme.IsTransient(status));

        UpdateTunnel(status);
        UpdateEgress(status);
        UpdateApps(status);

        _ticker.Enabled = status.Armed;
        UpdateStrip();
    }

    private void UpdateTunnel(GuardStatus status)
    {
        if (!status.Armed)
        {
            _tunnel.Set("Off", "Press the button to start", UiTheme.TextTertiary);
            return;
        }

        (string value, Color accent) = status.Verdict switch
        {
            GuardVerdict.Safe => ("Safe", UiTheme.Success),
            GuardVerdict.Leaking => ("Leaking", UiTheme.Critical),
            GuardVerdict.NotConfigured => ("Not set up", UiTheme.TextTertiary),
            _ => ("Unconfirmed", UiTheme.Caution)
        };

        int healthy = UiTheme.HealthySignalCount(status);
        string detail = status.Readings.Count == 0
            ? "No tunnel signal is configured"
            : $"{healthy} of {status.Readings.Count} signals healthy";

        _tunnel.Set(value, detail, accent);
    }

    private void UpdateEgress(GuardStatus status)
    {
        SignalReading? reading = UiTheme.Reading(status, "egress");

        if (reading is null)
        {
            _egress.Set("Not checked", "Compares the tunnel exit with your real exit", UiTheme.TextTertiary);
            return;
        }

        (string value, Color accent) = reading.Value.State switch
        {
            SignalState.Healthy => ("Tunneled", UiTheme.Success),
            SignalState.Down => ("Leaking", UiTheme.Critical),
            _ => ("Unknown", UiTheme.Caution)
        };

        _egress.Set(value, reading.Value.Detail, accent);
    }

    private void UpdateApps(GuardStatus status)
    {
        int guarded = status.ProtectedApps.Count;

        if (status.Blocking)
        {
            string blocked = status.BlockedCount == 0
                ? "Block failed"
                : (status.BlockedCount == 1 ? "1 app blocked" : $"{status.BlockedCount} apps blocked");
            _apps.Set(blocked, "Cut off from the internet right now", UiTheme.Critical);
            return;
        }

        string detail = status.RefusedTargets.Count > 0
            ? (status.RefusedTargets.Count == 1 ? "1 entry refused for safety" : $"{status.RefusedTargets.Count} entries refused for safety")
            : "All protected applications are safe to guard";

        _apps.Set(
            guarded == 0 ? "None" : $"{guarded} guarded",
            detail,
            guarded == 0 ? UiTheme.TextTertiary : UiTheme.Success);
    }

    private void UpdateStrip()
    {
        if (_last is null)
        {
            _strip.ForeColor = UiTheme.TextTertiary;
            _strip.Text = "Waiting for the first evaluation";
            return;
        }

        GuardStatus status = _last;

        if (!string.IsNullOrWhiteSpace(status.LastError))
        {
            _strip.ForeColor = UiTheme.Critical;
            _strip.Text = status.LastError;
            return;
        }

        _strip.ForeColor = UiTheme.TextTertiary;

        if (!status.Armed)
        {
            _strip.Text = "Protection is off \u2014 standing by";
            return;
        }

        string when = status.LastEvaluation == DateTimeOffset.MinValue
            ? "never"
            : status.LastEvaluation.ToString("HH:mm:ss");

        string verdictLabel = status.Verdict switch
        {
            GuardVerdict.Safe => "Healthy",
            GuardVerdict.Leaking => "Traffic leaking",
            GuardVerdict.NotConfigured => "Signals not configured",
            _ => "Verifying\u2026"
        };

        string streakLabel = status.SafeStreak == 1 ? "1 safe check in a row" : $"{status.SafeStreak} safe checks in a row";
        _strip.Text = $"Last check {when}  \u00B7  {verdictLabel}  \u00B7  {streakLabel}";
    }
}
