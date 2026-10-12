using System.Text.Json;
using System.Text.Json.Serialization;

namespace VpnKillSwitch;

public sealed class AppSettings
{
    public string ProxyHost { get; set; } = "127.0.0.1";

    public int ProxyPort { get; set; }

    public string ProxyScheme { get; set; } = "socks5";

    public string[] ProxyProcesses { get; set; } = [];

    public string[] AdapterPatterns { get; set; } = [];

    public bool CheckEgressIdentity { get; set; } = true;

    public string[] EgressServices { get; set; } = [.. TunnelSignals.DefaultEgressServices];

    public string HealthCheckUrl { get; set; } = string.Empty;

    public int SignalIntervalMs { get; set; } = 750;

    public int EgressIntervalMs { get; set; } = 45000;

    public int ProbeTimeoutMs { get; set; } = 900;

    public int RecoveryThreshold { get; set; } = 3;

    public NotificationStyle NotificationStyle { get; set; } = NotificationStyle.WindowsNotification;

    public List<ProtectedApp> ProtectedApps { get; set; } = [];

    public AppTheme Theme { get; set; } = AppTheme.Dark;

    public bool KnownAppsSeeded { get; set; }

    public bool TunnelAutoDetected { get; set; }

    public string SelectedSourceInterfaceName { get; set; } = string.Empty;

    public string SelectedDestinationInterfaceName { get; set; } = string.Empty;

    public string[] VpnProcessNames { get; set; } = [];

    public string[] VpnProgramPaths { get; set; } = [];

    public int MonitorIntervalMs { get; set; } = 500;

    public string PublicHealthCheckUrl { get; set; } = "https://www.google.com/generate_204";

    public string VpnHealthCheckUrl { get; set; } = "https://www.facebook.com/";

    public int HealthCheckIntervalMs { get; set; } = 1500;

    public int HealthCheckTimeoutMs { get; set; } = 1500;

    public int FailureThreshold { get; set; } = 2;

    public int RecoveryThresholdLegacy { get; set; } = 2;

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static AppSettings Load()
    {
        string path = FilePath;
        if (!File.Exists(path))
            return new AppSettings();

        AppSettings settings;
        try
        {
            string json = File.ReadAllText(path);
            settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            ApplyLegacy(settings, json);
        }
        catch (Exception ex)
        {
            Diagnostics.Write("settings", $"could not read {path}, using defaults: {ex.Message}");
            return new AppSettings();
        }

        return settings;
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            });

            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Diagnostics.Write("settings", $"could not save settings: {ex.Message}");
        }
    }

    public void AutoConfigureTunnel()
    {
        if (TunnelAutoDetected)
            return;

        string[] processes = TunnelSignals.DetectProxyProcesses();
        string[] adapters = TunnelSignals.DetectTunnelAdapters();

        if (ProxyProcesses.Length == 0 && processes.Length > 0)
            ProxyProcesses = processes;

        if (AdapterPatterns.Length == 0 && adapters.Length > 0)
            AdapterPatterns = adapters;

        TunnelAutoDetected = true;
        Diagnostics.Write(
            "settings",
            $"auto-detected tunnel: processes=[{string.Join(", ", processes)}] adapters=[{string.Join(", ", adapters)}] proxy port={ProxyPort}");
    }

    public bool HasAnySignal =>
        ProxyPort > 0 ||
        ProxyProcesses.Length > 0 ||
        AdapterPatterns.Length > 0 ||
        CheckEgressIdentity ||
        !string.IsNullOrWhiteSpace(HealthCheckUrl);

    private static void ApplyLegacy(AppSettings settings, string json)
    {
        LegacySettings? legacy;
        try
        {
            legacy = JsonSerializer.Deserialize<LegacySettings>(json);
        }
        catch (JsonException)
        {
            return;
        }

        if (legacy is null)
            return;

        if (string.IsNullOrWhiteSpace(settings.HealthCheckUrl) &&
            !string.IsNullOrWhiteSpace(legacy.VpnHealthCheckUrl) &&
            !legacy.VpnHealthCheckUrl.Contains("facebook.com", StringComparison.OrdinalIgnoreCase) &&
            !legacy.VpnHealthCheckUrl.Contains("x.com", StringComparison.OrdinalIgnoreCase))
        {
            settings.HealthCheckUrl = legacy.VpnHealthCheckUrl;
        }

        if (legacy.HealthCheckTimeoutMs > 0)
            settings.ProbeTimeoutMs = Math.Clamp(legacy.HealthCheckTimeoutMs, 300, 10000);

        if (legacy.HealthCheckIntervalMs > 0)
            settings.SignalIntervalMs = Math.Clamp(legacy.HealthCheckIntervalMs, 250, 10000);

        if (settings.RecoveryThreshold < 2)
            settings.RecoveryThreshold = 2;

        if (settings.ProxyProcesses.Length == 0 && legacy.VpnProcessNames.Length > 0)
            settings.ProxyProcesses = [.. legacy.VpnProcessNames.Select(n => Path.GetFileNameWithoutExtension(n) ?? n).Where(n => !string.IsNullOrWhiteSpace(n))];

        if (settings.AdapterPatterns.Length == 0 && !string.IsNullOrWhiteSpace(legacy.SelectedDestinationInterfaceName))
            settings.AdapterPatterns = [legacy.SelectedDestinationInterfaceName];
    }

    private sealed class LegacySettings
    {
        public string[] VpnProcessNames { get; set; } = [];

        public string[] VpnProgramPaths { get; set; } = [];

        public string SelectedDestinationInterfaceName { get; set; } = string.Empty;

        public string PublicHealthCheckUrl { get; set; } = string.Empty;

        public string VpnHealthCheckUrl { get; set; } = string.Empty;

        public int HealthCheckIntervalMs { get; set; }

        public int HealthCheckTimeoutMs { get; set; }

        public int FailureThreshold { get; set; }

        public int RecoveryThreshold { get; set; }
    }
}

public sealed class ProtectedApp
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public AppPathStrategy PathStrategy { get; set; } = AppPathStrategy.Exact;

    public string SearchRoot { get; set; } = string.Empty;

    public List<string> RelativePathPatterns { get; set; } = [];

    public static ProtectedApp FromPath(string path)
    {
        var app = new ProtectedApp
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = System.IO.Path.GetFileNameWithoutExtension(path),
            Path = path,
            Enabled = true
        };

        ApplicationPathResolver.ConfigureVersionAwareLookup(app);
        return app;
    }
}

public enum AppPathStrategy
{
    Exact,
    VersionAware
}

public enum NotificationStyle
{
    WindowsNotification,
    SoundAlert,
    Both,
    Silent
}
