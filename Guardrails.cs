namespace VpnKillSwitch;

internal sealed record RefusedTarget(string Name, string Path, string Reason);

internal sealed record GuardrailResult(IReadOnlyList<string> Allowed, IReadOnlyList<RefusedTarget> Refused);

internal static class Guardrails
{
    private static readonly string[] CriticalProcesses =
    [
        "explorer", "svchost", "winlogon", "csrss", "services", "lsass", "smss", "wininit", "dwm", "taskhostw",
        "sihost", "fontdrvhost", "ctfmon", "runtimebroker", "searchhost", "startmenuexperiencehost", "shellexperiencehost",
        "audiodg", "conhost", "dllhost", "spoolsv", "msiexec", "lsaiso", "wmiprvse", "securityhealthservice"
    ];

    private static readonly string[] ProtectedRoots =
    [
        @"\windows\system32\", @"\windows\syswow64\", @"\windows\winsxs\", @"\windows\servicing\"
    ];

    public static GuardrailResult Inspect(IEnumerable<ProtectedApp> apps, AppSettings settings)
    {
        var allowed = new List<string>();
        var refused = new List<RefusedTarget>();

        string self = Path.GetFullPath(Application.ExecutablePath);
        string[] proxyNames = [.. settings.ProxyProcesses.Select(n => Path.GetFileNameWithoutExtension(n) ?? n)];

        foreach (ProtectedApp app in apps.Where(a => a.Enabled))
        {
            foreach (string path in ApplicationPathResolver.ResolvePaths(app))
            {
                string? reason = Reject(path, self, proxyNames);
                if (reason is null)
                {
                    if (!allowed.Contains(path, StringComparer.OrdinalIgnoreCase))
                        allowed.Add(path);
                }
                else if (!refused.Any(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase)))
                {
                    refused.Add(new RefusedTarget(app.Name, path, reason));
                }
            }
        }

        return new GuardrailResult(allowed, refused);
    }

    private static string? Reject(string path, string self, string[] proxyNames)
    {
        if (string.Equals(path, self, StringComparison.OrdinalIgnoreCase))
            return "this is the kill switch itself";

        string name = Path.GetFileNameWithoutExtension(path);

        if (proxyNames.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase)))
            return "this is the tunnel client; blocking it would cut the connection you are protecting";

        if (CriticalProcesses.Any(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase)))
            return "this is a critical Windows process";

        string lower = path.ToLowerInvariant();
        if (ProtectedRoots.Any(root => lower.Contains(root)))
            return "this lives in a protected Windows system folder";

        return null;
    }
}
