using System.Runtime.InteropServices;

namespace VpnKillSwitch;

public sealed class FirewallResult
{
    public bool Success { get; init; }

    public int Applied { get; init; }

    public string Detail { get; init; } = string.Empty;

    public static FirewallResult Ok(int applied, string detail) => new() { Success = true, Applied = applied, Detail = detail };

    public static FirewallResult Fail(string detail) => new() { Success = false, Detail = detail };
}

internal sealed class FirewallClient
{
    private const string Group = "VpnKillSwitch";
    private const string SlotPrefix = "VpnKillSwitch.AppBlock.";
    private const string LegacySlotPrefix = "VpnKillSwitch_AppBlock_";
    private const int SlotCount = 64;

    private const int DirectionOut = 2;
    private const int ActionBlock = 0;
    private const int ProtocolAny = 256;
    private const int ProfileAll = 0x7FFFFFFF;

    private const int HResultAccessDenied = unchecked((int)0x80070005);
    private const int HResultNotFound = unchecked((int)0x80070002);

    private readonly object _gate = new();
    private readonly dynamic _policy;

    public FirewallClient()
    {
        Type? type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
        if (type is null)
            throw new InvalidOperationException("The Windows Firewall COM API (HNetCfg.FwPolicy2) is not registered.");

        _policy = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Could not create the Windows Firewall policy object.");
    }

    public static IReadOnlyList<string> SlotNames()
    {
        var names = new List<string>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
            names.Add(SlotPrefix + i.ToString("D2"));

        return names;
    }

    public bool TryReadState(out string detail)
    {
        try
        {
            int profiles = _policy.CurrentProfileTypes;
            int count = _policy.Rules.Count;
            detail = $"{count} firewall rules total, active profile mask {profiles}";
            return true;
        }
        catch (Exception ex)
        {
            detail = Describe("read firewall state", ex);
            return false;
        }
    }

    public IReadOnlyList<string> PresentSlots()
    {
        lock (_gate)
        {
            var present = new List<string>();
            foreach (string name in SlotNames())
            {
                if (Exists(name))
                    present.Add(name);
            }

            foreach (string legacy in LegacySlotNames())
            {
                if (Exists(legacy))
                    present.Add(legacy);
            }

            return present;
        }
    }

    public FirewallResult Apply(IReadOnlyList<string> programPaths)
    {
        lock (_gate)
        {
            try
            {
                RemoveAllLocked();
            }
            catch (Exception ex)
            {
                return FirewallResult.Fail(Describe("clear existing rules", ex));
            }

            if (programPaths.Count == 0)
                return FirewallResult.Ok(0, "no applications to block; all managed rules removed");

            var wanted = new List<string>();
            try
            {
                for (int i = 0; i < programPaths.Count && i < SlotCount; i++)
                {
                    string name = SlotPrefix + i.ToString("D2");
                    AddBlockRule(name, programPaths[i]);
                    wanted.Add(name);
                }
            }
            catch (Exception ex)
            {
                TryCleanup();
                return FirewallResult.Fail(Describe("create block rules", ex));
            }

            var missing = wanted.Where(name => !Exists(name)).ToList();
            if (missing.Count > 0)
            {
                TryCleanup();
                return FirewallResult.Fail(
                    $"the firewall accepted the rules but {missing.Count} of {wanted.Count} could not be read back ({string.Join(", ", missing.Take(3))})");
            }

            return FirewallResult.Ok(wanted.Count, $"blocking {wanted.Count} executable path(s)");
        }
    }

    public FirewallResult Clear()
    {
        lock (_gate)
        {
            try
            {
                RemoveAllLocked();
                return FirewallResult.Ok(0, "all managed rules removed");
            }
            catch (Exception ex)
            {
                return FirewallResult.Fail(Describe("clear rules", ex));
            }
        }
    }

    public int CountPresent()
    {
        lock (_gate)
        {
            int total = 0;
            foreach (string name in SlotNames())
            {
                if (Exists(name))
                    total++;
            }

            return total;
        }
    }

    private void RemoveAllLocked()
    {
        foreach (string name in SlotNames())
            RemoveIfPresent(name);

        foreach (string name in LegacySlotNames())
            RemoveIfPresent(name);
    }

    private void TryCleanup()
    {
        try
        {
            RemoveAllLocked();
        }
        catch (Exception)
        {
        }
    }

    private static IEnumerable<string> LegacySlotNames()
    {
        for (int i = 1; i <= SlotCount; i++)
            yield return LegacySlotPrefix + i.ToString("D2");
    }

    private void AddBlockRule(string name, string programPath)
    {
        Type? ruleType = Type.GetTypeFromProgID("HNetCfg.FwRule")
            ?? throw new InvalidOperationException("The Windows Firewall rule object (HNetCfg.FwRule) is not registered.");

        dynamic rule = Activator.CreateInstance(ruleType)
            ?? throw new InvalidOperationException("Could not create a firewall rule object.");

        rule.Name = name;
        rule.Description = "Outbound block created by VPN Kill Switch while the tunnel is not proven safe.";
        rule.Grouping = Group;
        rule.ApplicationName = programPath;
        rule.Protocol = ProtocolAny;
        rule.Direction = DirectionOut;
        rule.Action = ActionBlock;
        rule.Enabled = true;
        rule.Profiles = ProfileAll;
        rule.InterfaceTypes = "All";

        _policy.Rules.Add(rule);
    }

    private bool Exists(string name)
    {
        try
        {
            _ = _policy.Rules.Item(name);
            return true;
        }
        catch (Exception ex) when (ex.HResult == HResultNotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(Describe($"read rule {name}", ex), ex);
        }
    }

    private void RemoveIfPresent(string name)
    {
        if (!Exists(name))
            return;

        _policy.Rules.Remove(name);
    }

    private static string Describe(string step, Exception ex)
    {
        string hint = ex.HResult switch
        {
            HResultAccessDenied => " (the app is not running as administrator, or another policy blocks the change)",
            _ => string.Empty
        };

        return $"could not {step}: {ex.Message}{hint}";
    }
}
