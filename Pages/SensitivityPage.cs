namespace VpnKillSwitch;

internal sealed class SensitivityPage : PagePanel
{
    private readonly AppSettings _settings;

    public SensitivityPage(AppSettings settings)
    {
        _settings = settings;

        var header = new PageHeader(
            "Sensitivity",
            "How fast the guard reacts when the tunnel drops, and how carefully it lets go again.");

        var interval = new NumericUpDown();
        var egress = new NumericUpDown();
        var timeout = new NumericUpDown();
        var recovery = new NumericUpDown();

        Configure(interval, 200, 10000, settings.SignalIntervalMs, 50);
        Configure(egress, 5000, 600000, settings.EgressIntervalMs, 5000);
        Configure(timeout, 200, 10000, settings.ProbeTimeoutMs, 100);
        Configure(recovery, 2, 10, settings.RecoveryThreshold, 1);

        Control intervalRow = Row(
            "Check interval (ms)",
            "How often local tunnel signals are polled (e.g. 500 ms). Lower values react faster to disconnects.",
            interval);
        Control egressRow = Row(
            "Egress check interval (ms)",
            "How often external IP verification is queried (e.g. 45,000 ms = 45 seconds).",
            egress);
        Control timeoutRow = Row(
            "Probe timeout (ms)",
            "Maximum wait time for a signal probe before treating it as failed. Unanswered counts as unsafe.",
            timeout);
        Control recoveryRow = Row(
            "Safe checks before recovery (count)",
            "Number of consecutive healthy evaluations required before removing firewall block rules.",
            recovery);

        var notifyCombo = new ComboBox();
        notifyCombo.Items.AddRange(
        [
            "Windows notifications",
            "Sound / Beep alert",
            "Both (Notification & Sound)",
            "Silent (Tray icon only)"
        ]);
        notifyCombo.SelectedIndex = Math.Clamp((int)settings.NotificationStyle, 0, 3);
        notifyCombo.SelectedIndexChanged += (_, _) =>
            Store(() => settings.NotificationStyle = (NotificationStyle)notifyCombo.SelectedIndex);

        Control notifyRow = Row(
            "Notification style",
            "How to alert you when apps are blocked or restored: Windows notifications, audio sound/beep, both, or silent.",
            notifyCombo);

        interval.ValueChanged += (_, _) => Store(() => settings.SignalIntervalMs = (int)interval.Value);
        egress.ValueChanged += (_, _) => Store(() => settings.EgressIntervalMs = (int)egress.Value);
        timeout.ValueChanged += (_, _) => Store(() => settings.ProbeTimeoutMs = (int)timeout.Value);
        recovery.ValueChanged += (_, _) => Store(() => settings.RecoveryThreshold = (int)recovery.Value);

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 8, 0)
        };
        scrollPanel.Controls.Add(notifyRow);
        scrollPanel.Controls.Add(recoveryRow);
        scrollPanel.Controls.Add(timeoutRow);
        scrollPanel.Controls.Add(egressRow);
        scrollPanel.Controls.Add(intervalRow);

        PageLayout.Compose(
            this,
            scrollPanel,
            [header],
            []);
    }

    private void Store(Action apply)
    {
        apply();
        _settings.Save();
    }

    private static void Configure(NumericUpDown control, int min, int max, int value, int increment)
    {
        control.Minimum = min;
        control.Maximum = max;
        control.Value = Math.Min(max, Math.Max(min, value));
        control.Increment = increment;
    }

    private static Control Row(string title, string hint, Control inner)
    {
        FieldCard card = inner switch
        {
            NumericUpDown num => new FieldCard(title, hint, num) { FrameWidth = 240 },
            ComboBox combo => new FieldCard(title, hint, combo) { FrameWidth = 240 },
            _ => throw new ArgumentException()
        };

        var wrap = new Panel
        {
            Dock = DockStyle.Top,
            Height = 130,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 12)
        };
        card.Dock = DockStyle.Fill;
        wrap.Controls.Add(card);
        return wrap;
    }
}
