using System.Text;

namespace VpnKillSwitch;

internal sealed class DiagnosticsPage : PagePanel
{
    private readonly GuardController _guard;
    private readonly TextBox _text;
    private readonly FluentButton _refresh;
    private readonly FluentButton _clear;
    private readonly NoticeBar _notice = new() { Dock = DockStyle.Top };
    private readonly System.Windows.Forms.Timer _noticeTimer;
    private readonly System.Windows.Forms.Timer _ticker;

    private GuardStatus? _last;

    public DiagnosticsPage(GuardController guard)
    {
        _guard = guard;

        var header = new PageHeader(
            "Diagnostics",
            "Exactly what the guard looked at, and what it did about it.");

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };

        _refresh = Button("Refresh", FluentButtonStyle.Subtle, 110, (_, _) => RefreshView());
        _clear = Button("Clear firewall rules", FluentButtonStyle.Critical, 186, OnClearRules);
        actions.Controls.Add(_refresh);
        actions.Controls.Add(_clear);

        _text = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            BorderStyle = BorderStyle.None,
            BackColor = UiTheme.Input,
            ForeColor = UiTheme.Text,
            Font = new Font("Consolas", 8.5f),
            Dock = DockStyle.Fill
        };

        var host = new RoundedSurface { Dock = DockStyle.Fill, Padding = new Padding(14, 12, 8, 12) };
        host.Controls.Add(_text);

        _notice.VisibleChanged += (_, _) => PerformLayout();
        _noticeTimer = new System.Windows.Forms.Timer { Interval = 9000 };
        _noticeTimer.Tick += (_, _) => { _noticeTimer.Stop(); _notice.Hide(); };
        Disposed += (_, _) => _noticeTimer.Dispose();

        PageLayout.Compose(this, host, [header, _notice, actions], []);

        UiTheme.Bind(_text, () =>
        {
            _text.BackColor = UiTheme.Input;
            _text.ForeColor = UiTheme.Text;
        });

        _ticker = new System.Windows.Forms.Timer { Interval = 4000 };
        _ticker.Tick += (_, _) => RefreshView();
        _ticker.Start();
        Disposed += (_, _) => _ticker.Dispose();
    }

    public void SetStatus(GuardStatus status)
    {
        _last = status;
        Refresh();
    }

    private void RefreshView()
    {
        if (_text.IsDisposed)
            return;

        var builder = new StringBuilder();
        GuardStatus? status = _last;

        builder.AppendLine("GUARD");
        if (status is null)
        {
            builder.AppendLine("  no evaluation has run yet");
        }
        else
        {
            builder.AppendLine($"  armed            : {status.Armed}");
            builder.AppendLine($"  verdict          : {status.Verdict}");
            builder.AppendLine($"  blocking         : {status.Blocking} ({(status.BlockedCount == 1 ? "1 rule" : $"{status.BlockedCount} rules")} applied)");
            builder.AppendLine($"  safe checks in a row: {status.SafeStreak}");
            builder.AppendLine($"  last evaluation  : {(status.LastEvaluation == DateTimeOffset.MinValue ? "never" : status.LastEvaluation.ToString("yyyy-MM-dd HH:mm:ss"))}");
            builder.AppendLine($"  last error       : {status.LastError ?? "(none)"}");
        }

        builder.AppendLine();
        builder.AppendLine("SIGNALS");

        if (status is null || status.Readings.Count == 0)
        {
            builder.AppendLine("  none configured");
        }
        else
        {
            foreach (SignalReading reading in status.Readings)
                builder.AppendLine($"  [{reading.State,-7}] {reading.Name,-16} {reading.Detail}");
        }

        builder.AppendLine();
        builder.AppendLine("PROTECTED APPLICATIONS");

        if (status is null || status.ProtectedApps.Count == 0)
        {
            builder.AppendLine("  none enabled");
        }
        else
        {
            foreach (ProtectedApp app in status.ProtectedApps)
                builder.AppendLine($"  {app.Name,-22} {app.Path}");
        }

        if (status is not null && status.RefusedTargets.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("SAFETY GUARDRAILS (REFUSED TARGETS)");
            foreach (string refused in status.RefusedTargets)
                builder.AppendLine("  " + refused);
        }

        builder.AppendLine();
        builder.AppendLine($"LOG  ({Diagnostics.LogPath})");
        builder.AppendLine();

        string tail = Diagnostics.Tail(220);
        builder.AppendLine(tail.Length == 0 ? "  the log is empty" : tail);

        string wanted = builder.ToString();
        if (!string.Equals(_text.Text, wanted, StringComparison.Ordinal))
        {
            int selection = _text.SelectionStart;
            _text.Text = wanted;
            _text.SelectionStart = Math.Min(selection, _text.TextLength);
        }
    }

    private async void OnClearRules(object? sender, EventArgs e)
    {
        _clear.SetBusy(true, "Clearing\u2026");

        try
        {
            FirewallResult result = await _guard.ClearRulesAsync().ConfigureAwait(true);
            ShowNotice(result.Detail, result.Success ? NoticeKind.Success : NoticeKind.Failure);
        }
        catch (Exception ex)
        {
            ShowNotice(ex.Message, NoticeKind.Failure);
        }
        finally
        {
            _clear.SetBusy(false);
            Refresh();
        }
    }

    private void ShowNotice(string message, NoticeKind kind)
    {
        _notice.Show(message, kind);
        _noticeTimer.Stop();
        _noticeTimer.Start();
    }
}
