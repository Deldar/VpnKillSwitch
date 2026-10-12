namespace VpnKillSwitch;

internal sealed class TunnelPage : PagePanel
{
    private readonly AppSettings _settings;
    private readonly FlowLayoutPanel _body = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = Color.Transparent,
        Padding = new Padding(0, 0, 14, 0)
    };

    private readonly NoticeBar _notice = new() { Dock = DockStyle.Top };
    private readonly System.Windows.Forms.Timer _noticeTimer;
    private readonly List<Control> _cards = [];
    private readonly Label _derived = new();

    private readonly TextBox _host = new();
    private readonly NumericUpDown _port = new();
    private readonly TextBox _processes = new();
    private readonly TextBox _adapters = new();
    private readonly TextBox _healthUrl = new();
    private readonly FluentButton _egress;
    private readonly FluentButton _detect;

    public TunnelPage(AppSettings settings)
    {
        _settings = settings;

        var header = new PageHeader(
            "Tunnel",
            "Configure signals used by the kill switch to verify VPN tunnel connectivity.");

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };

        _detect = Button("Detect automatically", FluentButtonStyle.Accent, 176, OnDetect);
        _egress = Button(string.Empty, FluentButtonStyle.Subtle, 232, OnToggleEgress);
        actions.Controls.Add(_detect);
        actions.Controls.Add(_egress);

        _derived.Dock = DockStyle.Fill;
        _derived.TextAlign = ContentAlignment.MiddleLeft;
        _derived.Font = UiTheme.CaptionText;
        _derived.BackColor = Color.Transparent;
        actions.Controls.Add(_derived);

        _notice.VisibleChanged += (_, _) => PerformLayout();
        _noticeTimer = new System.Windows.Forms.Timer { Interval = 9000 };
        _noticeTimer.Tick += (_, _) => { _noticeTimer.Stop(); _notice.Hide(); };
        Disposed += (_, _) => _noticeTimer.Dispose();

        PageLayout.Compose(this, _body, [header, _notice, actions], []);

        BuildCards();
        _body.Resize += (_, _) => Reflow();
        UiTheme.Changed += Reflow;

        Reload();
    }

    public void Reload()
    {
        _host.Text = _settings.ProxyHost;
        _port.Value = Math.Clamp(_settings.ProxyPort, 0, 65535);
        _processes.Text = string.Join(", ", _settings.ProxyProcesses);
        _adapters.Text = string.Join(", ", _settings.AdapterPatterns);
        _healthUrl.Text = _settings.HealthCheckUrl;
        UpdateEgressButton();
        Reflow();
    }

    public void ShowNotice(string message, NoticeKind kind = NoticeKind.Warning)
    {
        _notice.Show(message, kind);
        _noticeTimer.Stop();
        _noticeTimer.Start();
    }

    private void BuildCards()
    {
        Add(Card("Proxy port", "The local port your VPN proxy listens on (e.g. 7890). Leave 0 to disable.", _port, 130));
        Add(Card("Proxy host", "Local host IP address (usually 127.0.0.1).", _host, 130));
        Add(Card("Watch these processes", "Comma-separated process names. If the VPN client exits, protected apps are blocked immediately.", _processes, 130));
        Add(Card("Tunnel adapters", "Comma-separated name fragments, matched against network adapter name and description.", _adapters, 130));
        Add(Card("Health endpoint (optional)", "Optional URL reachable only through the VPN tunnel.", _healthUrl, 130));
    }

    private void Add(Control card)
    {
        _cards.Add(card);
        _body.Controls.Add(card);
    }

    private Control Card(string title, string hint, Control editor, int height)
    {
        FieldCard card = editor is NumericUpDown number
            ? new FieldCard(title, hint, number) { FrameWidth = 220 }
            : new FieldCard(title, hint, (TextBox)editor);

        var wrap = new Panel
        {
            Height = height + 16,
            Width = 700,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 16)
        };

        card.Dock = DockStyle.Fill;
        wrap.Controls.Add(card);

        if (editor is TextBox textBox)
        {
            textBox.TextChanged += (_, _) => Store();
            textBox.Leave += (_, _) => Persist();
        }
        else if (editor is NumericUpDown numeric)
        {
            numeric.ValueChanged += (_, _) => Persist();
        }

        return wrap;
    }

    private void Store()
    {
        _settings.ProxyHost = string.IsNullOrWhiteSpace(_host.Text) ? "127.0.0.1" : _host.Text.Trim();
        _settings.HealthCheckUrl = _healthUrl.Text.Trim();
        _settings.ProxyProcesses = Split(_processes.Text);
        _settings.AdapterPatterns = Split(_adapters.Text);
        UpdateEgressButton();
    }

    private void Persist()
    {
        Store();
        _settings.ProxyPort = (int)_port.Value;
        _settings.ProxyScheme = SchemeForPort(_settings.ProxyPort);
        _settings.Save();
    }

    private void OnToggleEgress(object? sender, EventArgs e)
    {
        _settings.CheckEgressIdentity = !_settings.CheckEgressIdentity;
        _settings.Save();
        UpdateEgressButton();
        ShowNotice(
            _settings.CheckEgressIdentity
                ? "Egress identity check is on: the guard compares your tunnel exit with your real exit."
                : "Egress identity check is off. The guard now relies on the other signals only.",
            NoticeKind.Success);
    }

    private async void OnDetect(object? sender, EventArgs e)
    {
        _detect.SetBusy(true, "Detecting\u2026");

        try
        {
            string host = string.IsNullOrWhiteSpace(_host.Text) ? "127.0.0.1" : _host.Text.Trim();
            int timeout = Math.Clamp(_settings.ProbeTimeoutMs, 200, 2000);

            string[] processes = TunnelSignals.DetectProxyProcesses();
            string[] adapters = TunnelSignals.DetectTunnelAdapters();
            int? port = await Task.Run(() => TunnelSignals
                .DetectProxyPortAsync(host, timeout, CancellationToken.None))
                .ConfigureAwait(true);

            if (processes.Length > 0)
                _settings.ProxyProcesses = processes;

            if (adapters.Length > 0)
                _settings.AdapterPatterns = adapters;

            if (port is not null)
            {
                _settings.ProxyPort = port.Value;
                _settings.ProxyScheme = SchemeForPort(port.Value);
            }

            _settings.TunnelAutoDetected = true;
            _settings.Save();
            Reload();

            var parts = new List<string>();
            if (port is not null)
                parts.Add($"proxy on port {port}");
            if (processes.Length > 0)
                parts.Add(processes.Length == 1 ? "1 client process" : $"{processes.Length} client processes");
            if (adapters.Length > 0)
                parts.Add(adapters.Length == 1 ? "1 tunnel adapter" : $"{adapters.Length} tunnel adapters");

            ShowNotice(
                parts.Count > 0
                    ? "Found " + string.Join(", ", parts) + "."
                    : "Nothing was detected. Start your VPN client first, or fill the fields in by hand.",
                parts.Count > 0 ? NoticeKind.Success : NoticeKind.Warning);
        }
        catch (Exception ex)
        {
            ShowNotice(ex.Message, NoticeKind.Failure);
        }
        finally
        {
            _detect.SetBusy(false);
        }
    }

    private void UpdateEgressButton()
    {
        _egress.Text = _settings.CheckEgressIdentity ? "Egress check: On" : "Egress check: Off";
        _egress.Kind = _settings.CheckEgressIdentity ? FluentButtonStyle.Accent : FluentButtonStyle.Subtle;

        var parts = new List<string>();
        if (_settings.ProxyPort > 0)
            parts.Add($"proxy {_settings.ProxyHost}:{_settings.ProxyPort} ({_settings.ProxyScheme})");
        if (_settings.ProxyProcesses.Length > 0)
            parts.Add(_settings.ProxyProcesses.Length == 1 ? "1 process" : $"{_settings.ProxyProcesses.Length} processes");
        if (_settings.AdapterPatterns.Length > 0)
            parts.Add(_settings.AdapterPatterns.Length == 1 ? "1 adapter" : $"{_settings.AdapterPatterns.Length} adapters");
        if (_settings.CheckEgressIdentity)
            parts.Add("egress identity");
        if (!string.IsNullOrWhiteSpace(_settings.HealthCheckUrl))
            parts.Add("health endpoint");

        _derived.Text = parts.Count == 0
            ? "No signal is configured yet."
            : "Active signals: " + string.Join("  \u00B7  ", parts);
    }

    private void Reflow()
    {
        if (_body.IsDisposed)
            return;

        int width = Math.Max(360,
            _body.ClientSize.Width - _body.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);

        foreach (Control card in _cards)
            card.Width = width;

        _body.PerformLayout();
    }

    private static string[] Split(string value) =>
        [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private static string SchemeForPort(int port) => port switch
    {
        7890 or 7897 or 10809 or 2080 or 8080 or 8889 or 1087 => "http",
        _ => "socks5"
    };
}
