namespace VpnKillSwitch;

internal sealed class InfoPage : PagePanel
{
    private readonly FlowLayoutPanel _body = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = Color.Transparent,
        Padding = new Padding(0, 0, 14, 0)
    };

    private readonly List<Label> _wrapped = [];
    private readonly List<Panel> _rules = [];

    public InfoPage()
    {
        var header = new PageHeader(
            "Guide",
            "How the kill switch protects your traffic, detects tunnel health, and manages applications.");

        PageLayout.Compose(this, _body, [header], []);
        Build();

        _body.Resize += (_, _) => Reflow();
        UiTheme.Changed += Reflow;
        Disposed += (_, _) => UiTheme.Changed -= Reflow;
    }

    private void Build()
    {
        Heading("What it protects");
        Body("The kill switch prevents your real IP and network traffic from leaking if your VPN tunnel drops. "
            + "When the connection becomes unconfirmed or leaks, outbound traffic for your selected applications "
            + "is immediately blocked using Windows Firewall rules. Only managed rules created by this app "
            + "(under the VpnKillSwitch.Managed group) are touched.");

        Rule();

        Heading("How tunnel health is decided");
        Body("The guard continuously evaluates multiple configurable signals to determine whether traffic is safely tunneled:");
        Bullet("Local proxy port: Probes whether your local VPN/proxy port (e.g. 127.0.0.1:7890) is actively listening.");
        Bullet("Process monitor: Watches your VPN client processes; if the client closes or crashes, applications are blocked immediately.");
        Bullet("Network adapters: Verifies that your virtual VPN network adapter is active and operational.");
        Bullet("Egress identity: Compares external exit IP addresses to verify traffic exits via the VPN rather than your direct ISP.");
        Bullet("Network-change events: Windows network-change events trigger an instant re-check, reacting in milliseconds without waiting for a timer.");

        Rule();

        Heading("Sensitivity and fine tuning");
        Body("Configure timing parameters on the Sensitivity page:");
        Bullet("Check interval (ms): How often local signals are polled (typically 200–2,000 ms).");
        Bullet("Probe timeout (ms): Maximum time to wait for a signal probe before treating it as failed.");
        Bullet("Egress check interval (ms): How often external IP identity is verified (typically 30,000–60,000 ms).");
        Bullet("Safe checks before recovery: Number of consecutive safe checks required before unblocking apps.");

        Rule();

        Heading("Auto-detection & application updates");
        Body("The kill switch is built for modern AI developer tools and IDEs:");
        Bullet("Detect installed: Click 'Detect installed' on the Applications page to auto-discover popular IDEs and AI tools (Antigravity, Cursor, VS Code, Zed, Windsurf, Claude, JetBrains, ChatGPT, etc.).");
        Bullet("Auto-update support: Store packages and versioned apps (like Claude or Cursor) update into new directories. The kill switch detects the newest version automatically on startup and during monitoring.");
        Bullet("Custom apps & drag-and-drop: You can drag and drop any .exe or .lnk shortcut into the window, or enter custom paths manually.");

        Rule();

        Heading("Audio alerts & notifications");
        Body("Configure how you are notified when applications are blocked or restored (on the Sensitivity page):");
        Bullet("Windows notifications: Standard balloon/toast notifications from the system tray.");
        Bullet("Sound / Beep alert: Distinct synthesized audio tones (a clear descending warning alert when disconnected/blocked, and an ascending chime when reconnected/restored) without visual interruptions.");
        Bullet("Both (Notification & Sound): Delivers both audio chimes and Windows notifications.");
        Bullet("Silent: Silent operation with status visible via the system tray icon.");

        Rule();

        Heading("Tray icon colors");
        Legend(TrayIconSet.IdleAccent, "Gray \u2014 Protection is off (idle).");
        Legend(TrayIconSet.ConnectingAccent, "Amber \u2014 Verifying tunnel health (apps stay blocked until confirmed safe).");
        Legend(TrayIconSet.ConnectedAccent, "Green \u2014 Protection active and traffic is securely tunneled.");
        Legend(TrayIconSet.BlockedAccent, "Red \u2014 Traffic is leaking or unconfirmed; protected apps are blocked from the internet.");

        Rule();

        Heading("Getting started");
        Bullet("1. Open Applications and verify your protected IDEs and tools (or click 'Detect installed').");
        Bullet("2. Open Tunnel and click 'Detect automatically' to discover your running VPN client proxy and adapters.");
        Bullet("3. Turn on protection using the main toggle on the Dashboard.");
        Bullet("4. When the VPN reconnects, the app automatically detects it and unblocks applications within seconds.");
        Bullet("5. Use 'Check now' on the sidebar at any time to verify tunnel signals on demand.");
        Bullet("6. If you ever need to remove all firewall rules created by this app, click 'Clear firewall rules'.");
    }

    private void Reflow()
    {
        if (_body.IsDisposed)
            return;

        int width = Math.Max(280,
            _body.ClientSize.Width - _body.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);

        foreach (Label label in _wrapped)
            label.MaximumSize = new Size(width, 0);

        foreach (Panel rule in _rules)
            rule.Width = width;

        _body.PerformLayout();
    }

    private void Heading(string text)
    {
        var label = new Label
        {
            Text = text,
            Font = UiTheme.Section,
            AutoSize = true,
            Margin = new Padding(0, 22, 0, 0),
            BackColor = Color.Transparent
        };
        UiTheme.Bind(label, () => label.ForeColor = UiTheme.Text);
        _wrapped.Add(label);
        _body.Controls.Add(label);
    }

    private void Body(string text) => Wrap(text, UiTheme.Body, () => UiTheme.TextSecondary, 8, string.Empty);

    private void Bullet(string text) => Wrap(text, UiTheme.Body, () => UiTheme.TextSecondary, 4, "\u2022   ");

    private void Wrap(string text, Font font, Func<Color> color, int topMargin, string prefix)
    {
        var label = new Label
        {
            Text = prefix + text,
            Font = font,
            AutoSize = true,
            MaximumSize = new Size(600, 0),
            Margin = new Padding(prefix.Length > 0 ? 14 : 0, topMargin, 0, 0),
            BackColor = Color.Transparent
        };
        UiTheme.Bind(label, () => label.ForeColor = color());
        _wrapped.Add(label);
        _body.Controls.Add(label);
    }

    private void Legend(Color accent, string text)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(14, 6, 0, 0),
            BackColor = Color.Transparent
        };

        var dot = new Label
        {
            Text = "\u25CF",
            Font = UiTheme.BodyStrong,
            ForeColor = accent,
            AutoSize = true,
            Margin = new Padding(0, 0, 6, 0),
            BackColor = Color.Transparent
        };

        var caption = new Label
        {
            Text = text,
            Font = UiTheme.Body,
            AutoSize = true,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        UiTheme.Bind(caption, () => caption.ForeColor = UiTheme.TextSecondary);

        row.Controls.Add(dot);
        row.Controls.Add(caption);
        _body.Controls.Add(row);
    }

    private void Rule()
    {
        var line = new Panel
        {
            Height = 1,
            Width = 560,
            Margin = new Padding(0, 20, 0, 0)
        };
        UiTheme.Bind(line, () => line.BackColor = UiTheme.Stroke);
        _rules.Add(line);
        _body.Controls.Add(line);
    }
}
