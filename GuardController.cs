using System.Net.NetworkInformation;

namespace VpnKillSwitch;

public sealed record GuardStatus(
    bool Armed,
    bool Blocking,
    GuardVerdict Verdict,
    IReadOnlyList<SignalReading> Readings,
    IReadOnlyList<ProtectedApp> ProtectedApps,
    IReadOnlyList<string> RefusedTargets,
    int SafeStreak,
    int BlockedCount,
    string? LastError,
    DateTimeOffset LastEvaluation);

public sealed class GuardController : IDisposable
{
    private readonly AppSettings _settings;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _wake = new(0);
    private readonly object _statusGate = new();

    private FirewallClient? _firewall;
    private CancellationTokenSource? _run;
    private Task? _loop;
    private bool _armed;
    private bool _disposed;

    private GuardVerdict _verdict = GuardVerdict.NotConfigured;
    private IReadOnlyList<SignalReading> _readings = [];
    private IReadOnlyList<string> _refused = [];
    private SignalReading? _egressReading;
    private DateTimeOffset _egressAt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastEvaluation = DateTimeOffset.MinValue;
    private List<string> _appliedPaths = [];
    private bool _blocking;
    private int _safeStreak;
    private string? _lastError;
    private string? _firewallUnavailable;
    private DateTimeOffset _lastPathRefreshAt = DateTimeOffset.MinValue;

    public GuardController(AppSettings settings)
    {
        _settings = settings;

        try
        {
            _firewall = new FirewallClient();
        }
        catch (Exception ex)
        {
            _firewallUnavailable = ex.Message;
            Diagnostics.Write("firewall", "unavailable: " + ex.Message);
        }

        NetworkChange.NetworkAddressChanged += OnNetworkChanged;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
    }

    public event Action<GuardStatus>? StatusChanged;

    public AppSettings Settings => _settings;

    public bool IsArmed
    {
        get
        {
            lock (_statusGate)
                return _armed;
        }
    }

    public void RecoverOrphanedRules()
    {
        if (_firewall is null)
            return;

        try
        {
            IReadOnlyList<string> present = _firewall.PresentSlots();
            if (present.Count == 0)
                return;

            Diagnostics.Write("firewall", $"found {present.Count} block rule(s) left by a previous run; removing them");
            FirewallResult result = _firewall.Clear();
            Diagnostics.Write("firewall", "orphan cleanup: " + result.Detail);
        }
        catch (Exception ex)
        {
            Diagnostics.Write("firewall", "orphan cleanup failed: " + ex.Message);
        }
    }

    public async Task ArmAsync()
    {
        if (_disposed)
            return;

        _settings.AutoConfigureTunnel();
        try
        {
            RefreshReport report = KnownApps.Refresh(_settings.ProtectedApps);
            if (report.Repathed > 0)
            {
                Diagnostics.Write("guard", $"auto-updated paths before arming: {report.Describe()}");
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Write("guard", $"path refresh error: {ex.Message}");
        }
        _settings.Save();

        lock (_statusGate)
        {
            if (_armed)
                return;

            _armed = true;
            _safeStreak = 0;
            _lastError = null;
        }

        Diagnostics.Write("guard", "armed");

        _run = new CancellationTokenSource();
        _loop = Task.Run(() => LoopAsync(_run.Token));
        await EvaluateAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task DisarmAsync()
    {
        CancellationTokenSource? run;
        Task? loop;

        lock (_statusGate)
        {
            if (!_armed)
                return;

            _armed = false;
            run = _run;
            loop = _loop;
            _run = null;
            _loop = null;
        }

        try
        {
            run?.Cancel();
            if (loop is not null)
                await loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        run?.Dispose();

        if (await EnterGateAsync().ConfigureAwait(false))
        {
            try
            {
                Unblock("protection disarmed");
            }
            finally
            {
                _gate.Release();
            }
        }

        Diagnostics.Write("guard", "disarmed");
        Publish();
    }

    public async Task<FirewallResult> ClearRulesAsync()
    {
        if (_firewall is null)
            return FirewallResult.Fail(_firewallUnavailable ?? "the firewall API is unavailable");

        if (!await EnterGateAsync().ConfigureAwait(false))
            return FirewallResult.Fail("the guard is shutting down");

        try
        {
            FirewallResult result = _firewall.Clear();
            if (result.Success)
            {
                _blocking = false;
                _appliedPaths = [];
            }

            Diagnostics.Write("firewall", "manual clear: " + result.Detail);
            return result;
        }
        finally
        {
            _gate.Release();
            Publish();
        }
    }

    public async Task EvaluateAsync(CancellationToken cancellationToken, bool forceFreshEgress = false)
    {
        if (_disposed || _firewall is null)
        {
            if (_firewall is null)
                SetError(_firewallUnavailable ?? "the firewall API is unavailable");

            return;
        }

        if (!await EnterGateAsync(cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            if (forceFreshEgress)
            {
                _egressReading = null;
                _egressAt = DateTimeOffset.MinValue;
            }

            List<SignalReading> readings = await CollectAsync(cancellationToken, forceFreshEgress).ConfigureAwait(false);
            GuardVerdict verdict = Compute(readings);

            lock (_statusGate)
            {
                _readings = readings;
                _verdict = verdict;
                _lastEvaluation = DateTimeOffset.Now;
            }

            Apply(verdict);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            Diagnostics.Write("guard", "evaluation failed: " + ex.Message);
        }
        finally
        {
            _gate.Release();
            Publish();
        }
    }

    public GuardStatus GetStatus()
    {
        lock (_statusGate)
        {
            return new GuardStatus(
                _armed,
                _blocking,
                _verdict,
                _readings,
                _settings.ProtectedApps.Where(app => app.Enabled).ToArray(),
                _refused,
                _safeStreak,
                _appliedPaths.Count,
                _lastError,
                _lastEvaluation);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkChanged;

        try
        {
            DisarmAsync().GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        _run?.Dispose();
        _gate.Dispose();
        _wake.Dispose();
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (DateTimeOffset.Now - _lastPathRefreshAt > TimeSpan.FromMinutes(5) ||
                _settings.ProtectedApps.Any(a => a.Enabled && !File.Exists(a.Path)))
            {
                _lastPathRefreshAt = DateTimeOffset.Now;
                try
                {
                    RefreshReport report = KnownApps.Refresh(_settings.ProtectedApps);
                    if (report.Repathed > 0)
                    {
                        _settings.Save();
                        Diagnostics.Write("guard", $"background auto-updated paths: {report.Describe()}");
                    }
                }
                catch (Exception ex)
                {
                    Diagnostics.Write("guard", $"background path refresh error: {ex.Message}");
                }
            }

            await EvaluateAsync(cancellationToken).ConfigureAwait(false);

            int delay = Math.Clamp(_settings.SignalIntervalMs, 200, 10000);
            if (_blocking)
                delay = Math.Clamp(delay / 2, 400, 1500);

            try
            {
                await Task.WhenAny(
                    Task.Delay(delay, cancellationToken),
                    _wake.WaitAsync(cancellationToken)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task<List<SignalReading>> CollectAsync(CancellationToken cancellationToken, bool forceFreshEgress = false)
    {
        var readings = new List<SignalReading>();
        int timeout = Math.Clamp(_settings.ProbeTimeoutMs, 200, 10000);

        if (_settings.ProxyPort > 0)
        {
            readings.Add(await TunnelSignals
                .ProxyPortAsync(_settings.ProxyHost, _settings.ProxyPort, timeout, cancellationToken)
                .ConfigureAwait(false));
        }

        if (_settings.ProxyProcesses.Length > 0)
            readings.Add(TunnelSignals.ProxyProcess(_settings.ProxyProcesses));

        if (_settings.AdapterPatterns.Length > 0)
            readings.Add(TunnelSignals.TunnelAdapters(_settings.AdapterPatterns));

        if (!string.IsNullOrWhiteSpace(_settings.HealthCheckUrl))
        {
            readings.Add(await TunnelSignals
                .HealthEndpointAsync(_settings.HealthCheckUrl, timeout, cancellationToken)
                .ConfigureAwait(false));
        }

        if (_settings.CheckEgressIdentity && _settings.EgressServices.Length > 0)
            readings.Add(await EgressReadingAsync(cancellationToken, forceFreshEgress).ConfigureAwait(false));

        return readings;
    }

    private async Task<SignalReading> EgressReadingAsync(CancellationToken cancellationToken, bool forceFresh = false)
    {
        int interval = Math.Clamp(_settings.EgressIntervalMs, 5000, 600000);

        if (_blocking || _egressReading?.State != SignalState.Healthy)
        {
            interval = Math.Clamp(_settings.SignalIntervalMs * 2, 2000, 4000);
        }

        TimeSpan age = DateTimeOffset.Now - _egressAt;

        if (!forceFresh && _egressReading is not null && age < TimeSpan.FromMilliseconds(interval))
            return _egressReading.Value;

        if (!forceFresh && _egressReading is not null && age > TimeSpan.FromMilliseconds(interval * 2L))
        {
            return SignalReading.Unknown(
                "egress identity",
                $"the last check is {age.TotalSeconds:F0} s old, which is too stale to trust");
        }

        SignalReading fresh = await TunnelSignals
            .EgressIdentityAsync(
                _settings.ProxyHost,
                _settings.ProxyPort,
                _settings.ProxyScheme,
                _settings.EgressServices,
                Math.Max(3000, Math.Clamp(_settings.ProbeTimeoutMs, 200, 10000)),
                cancellationToken)
            .ConfigureAwait(false);

        _egressReading = fresh;
        _egressAt = DateTimeOffset.Now;
        return fresh;
    }

    private static GuardVerdict Compute(IReadOnlyList<SignalReading> readings)
    {
        if (readings.Count == 0)
            return GuardVerdict.NotConfigured;

        if (readings.Any(reading => reading.State == SignalState.Down))
            return GuardVerdict.Leaking;

        return readings.All(reading => reading.State == SignalState.Healthy)
            ? GuardVerdict.Safe
            : GuardVerdict.Unknown;
    }

    private void Apply(GuardVerdict verdict)
    {
        switch (verdict)
        {
            case GuardVerdict.NotConfigured:
                SetError("nothing is configured to detect the tunnel, so no decision can be made");
                _safeStreak = 0;
                break;

            case GuardVerdict.Safe:
                _safeStreak++;
                if (_blocking && _safeStreak >= Math.Max(2, _settings.RecoveryThreshold))
                    Unblock($"the tunnel looked healthy {_safeStreak} times in a row");

                break;

            default:
                _safeStreak = 0;
                Block(verdict == GuardVerdict.Leaking
                    ? "traffic is not going through the tunnel"
                    : "the tunnel state could not be confirmed");
                break;
        }
    }

    private void Block(string reason)
    {
        GuardrailResult inspection = Guardrails.Inspect(_settings.ProtectedApps, _settings);

        lock (_statusGate)
        {
            _refused = inspection.Refused.Select(r => $"{r.Name}: {r.Reason}").ToArray();
        }

        if (inspection.Allowed.Count == 0)
        {
            SetError("no protectable application is configured");
            return;
        }

        if (_blocking && inspection.Allowed.Count == _appliedPaths.Count &&
            !inspection.Allowed.Except(_appliedPaths, StringComparer.OrdinalIgnoreCase).Any())
        {
            return;
        }

        FirewallResult result = _firewall!.Apply(inspection.Allowed);

        lock (_statusGate)
        {
            _blocking = result.Success;
            _appliedPaths = result.Success ? [.. inspection.Allowed] : [];
            _lastError = result.Success ? null : "BLOCKING FAILED: " + result.Detail;
        }

        Diagnostics.Write("firewall", $"block ({reason}): {result.Detail}");
    }

    private void Unblock(string reason)
    {
        FirewallResult result = _firewall!.Clear();

        lock (_statusGate)
        {
            if (result.Success)
            {
                _blocking = false;
                _appliedPaths = [];
                _safeStreak = 0;
                _lastError = null;
            }
            else
            {
                _lastError = "UNBLOCKING FAILED: " + result.Detail;
            }
        }

        Diagnostics.Write("firewall", $"unblock ({reason}): {result.Detail}");
    }

    private async Task<bool> EnterGateAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return false;

        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private void OnNetworkChanged(object? sender, EventArgs e)
    {
        if (!IsArmed)
            return;

        Diagnostics.Write("network", "adapter change detected; re-evaluating now");

        _egressReading = null;
        _egressAt = DateTimeOffset.MinValue;

        try
        {
            if (_wake.CurrentCount == 0)
                _wake.Release();
        }
        catch (Exception)
        {
        }
    }

    private void SetError(string message)
    {
        lock (_statusGate)
            _lastError = message;
    }

    private void Publish()
    {
        GuardStatus status = GetStatus();
        StatusChanged?.Invoke(status);
    }
}
