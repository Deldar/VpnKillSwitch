namespace VpnKillSwitch;

internal sealed record KnownApp(string Name, string[] Candidates);

internal readonly record struct RefreshReport(int Repathed, int Missing)
{
    public string Describe()
    {
        if (Repathed == 0 && Missing == 0)
            return "Every stored path is still valid.";

        var parts = new List<string>();
        if (Repathed > 0)
            parts.Add($"{Repathed} path{(Repathed == 1 ? string.Empty : "s")} updated");
        if (Missing > 0)
            parts.Add($"{Missing} could not be found on disk");

        return string.Join("  \u00B7  ", parts) + ".";
    }
}

internal static class KnownApps
{
    public static readonly KnownApp[] Catalog =
    [
        new("Antigravity", [
            @"%LOCALAPPDATA%\Programs\antigravity\Antigravity.exe",
            @"%PROGRAMFILES%\antigravity\Antigravity.exe",
            @"%LOCALAPPDATA%\antigravity\Antigravity.exe"
        ]),
        new("Cursor", [
            @"%LOCALAPPDATA%\Programs\cursor\Cursor.exe",
            @"%LOCALAPPDATA%\Programs\Cursor\Cursor.exe",
            @"%PROGRAMFILES%\Cursor\Cursor.exe"
        ]),
        new("Visual Studio Code", [
            @"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe",
            @"%PROGRAMFILES%\Microsoft VS Code\Code.exe"
        ]),
        new("VS Code Insiders", [
            @"%LOCALAPPDATA%\Programs\Microsoft VS Code Insiders\Code - Insiders.exe",
            @"%PROGRAMFILES%\Microsoft VS Code Insiders\Code - Insiders.exe"
        ]),
        new("VSCodium", [
            @"%LOCALAPPDATA%\Programs\VSCodium\VSCodium.exe",
            @"%PROGRAMFILES%\VSCodium\VSCodium.exe"
        ]),
        new("Zed", [
            @"%LOCALAPPDATA%\Zed\zed.exe",
            @"%LOCALAPPDATA%\Programs\Zed\zed.exe",
            @"%LOCALAPPDATA%\Zed Preview\zed.exe",
            @"%PROGRAMFILES%\Zed\zed.exe"
        ]),
        new("Windsurf", [
            @"%LOCALAPPDATA%\Programs\Windsurf\Windsurf.exe",
            @"%PROGRAMFILES%\Windsurf\Windsurf.exe"
        ]),
        new("Claude", [
            @"%PROGRAMFILES%\WindowsApps\Claude_*\app\claude.exe",
            @"%LOCALAPPDATA%\Microsoft\WindowsApps\Claude_*\claude*.exe",
            @"%LOCALAPPDATA%\Microsoft\WindowsApps\claude-desktop.exe",
            @"%LOCALAPPDATA%\AnthropicClaude\*\claude.exe",
            @"%LOCALAPPDATA%\Programs\Claude\Claude.exe"
        ]),
        new("Claude Code", [
            @"%USERPROFILE%\.local\bin\claude.exe",
            @"%LOCALAPPDATA%\Programs\claude\claude.exe",
            @"%APPDATA%\npm\claude.cmd",
            @"%APPDATA%\npm\claude.exe"
        ]),
        new("IntelliJ IDEA", [
            @"%LOCALAPPDATA%\Programs\IntelliJ IDEA*\bin\idea64.exe",
            @"%PROGRAMFILES%\JetBrains\IntelliJ IDEA*\bin\idea64.exe",
            @"%LOCALAPPDATA%\JetBrains\Toolbox\apps\IDEA-*\*\bin\idea64.exe"
        ]),
        new("PyCharm", [
            @"%LOCALAPPDATA%\Programs\PyCharm*\bin\pycharm64.exe",
            @"%PROGRAMFILES%\JetBrains\PyCharm*\bin\pycharm64.exe",
            @"%LOCALAPPDATA%\JetBrains\Toolbox\apps\PyCharm-*\*\bin\pycharm64.exe"
        ]),
        new("WebStorm", [
            @"%LOCALAPPDATA%\Programs\WebStorm*\bin\webstorm64.exe",
            @"%PROGRAMFILES%\JetBrains\WebStorm*\bin\webstorm64.exe",
            @"%LOCALAPPDATA%\JetBrains\Toolbox\apps\WebStorm\*\bin\webstorm64.exe"
        ]),
        new("Rider", [
            @"%LOCALAPPDATA%\Programs\Rider*\bin\rider64.exe",
            @"%PROGRAMFILES%\JetBrains\Rider*\bin\rider64.exe",
            @"%LOCALAPPDATA%\JetBrains\Toolbox\apps\Rider\*\bin\rider64.exe"
        ]),
        new("CLion", [
            @"%LOCALAPPDATA%\Programs\CLion*\bin\clion64.exe",
            @"%PROGRAMFILES%\JetBrains\CLion*\bin\clion64.exe",
            @"%LOCALAPPDATA%\JetBrains\Toolbox\apps\CLion\*\bin\clion64.exe"
        ]),
        new("GoLand", [
            @"%LOCALAPPDATA%\Programs\GoLand*\bin\goland64.exe",
            @"%PROGRAMFILES%\JetBrains\GoLand*\bin\goland64.exe",
            @"%LOCALAPPDATA%\JetBrains\Toolbox\apps\Goland\*\bin\goland64.exe"
        ]),
        new("Fleet", [
            @"%LOCALAPPDATA%\Programs\Fleet\Fleet.exe",
            @"%LOCALAPPDATA%\JetBrains\Toolbox\apps\Fleet\*\Fleet.exe"
        ]),
        new("Android Studio", [
            @"%PROGRAMFILES%\Android\Android Studio\bin\studio64.exe",
            @"%LOCALAPPDATA%\Programs\Android Studio\bin\studio64.exe"
        ]),
        new("Sublime Text", [
            @"%PROGRAMFILES%\Sublime Text\sublime_text.exe",
            @"%PROGRAMFILES%\Sublime Text 3\sublime_text.exe"
        ]),
        new("ChatGPT", [
            @"%PROGRAMFILES%\WindowsApps\*ChatGPT*\app\ChatGPT.exe",
            @"%PROGRAMFILES%\WindowsApps\*OpenAI*\app\ChatGPT.exe",
            @"%LOCALAPPDATA%\Programs\ChatGPT\ChatGPT.exe",
            @"%LOCALAPPDATA%\ChatGPT\ChatGPT.exe"
        ]),
        new("Ollama", [
            @"%LOCALAPPDATA%\Programs\Ollama\ollama.exe",
            @"%LOCALAPPDATA%\Ollama\ollama.exe"
        ]),
        new("LM Studio", [
            @"%LOCALAPPDATA%\Programs\LM Studio\LM Studio.exe",
            @"%LOCALAPPDATA%\LM-Studio\LM Studio.exe"
        ]),
        new("Postman", [
            @"%LOCALAPPDATA%\Postman\Postman.exe",
            @"%LOCALAPPDATA%\Programs\Postman\Postman.exe"
        ]),
        new("GitHub Desktop", [
            @"%LOCALAPPDATA%\GitHubDesktop\GitHubDesktop.exe",
            @"%LOCALAPPDATA%\GitHub Desktop\GitHubDesktop.exe"
        ]),
        new("Docker Desktop", [
            @"%PROGRAMFILES%\Docker\Docker\Docker Desktop.exe"
        ])
    ];

    public static (int added, List<string> names) ScanAndAdd(List<ProtectedApp> apps)
    {
        int added = 0;
        var names = new List<string>();
        foreach (KnownApp known in Catalog)
        {
            if (ContainsName(apps, known.Name))
                continue;

            string? path = Resolve(known);
            if (path is null || Contains(apps, path))
                continue;

            apps.Add(Create(known.Name, path));
            names.Add(known.Name);
            added++;
        }

        return (added, names);
    }

    public static int Seed(List<ProtectedApp> apps)
    {
        var (added, _) = ScanAndAdd(apps);
        return added;
    }

    public static RefreshReport Refresh(List<ProtectedApp> apps)
    {
        int repathed = 0;
        int missing = 0;

        foreach (ProtectedApp app in apps)
        {
            if (app.PathStrategy == AppPathStrategy.Exact)
                ApplicationPathResolver.ConfigureVersionAwareLookup(app);

            string? newest = NewestMatch(app);

            if (newest is null && (FindByName(app.Name) ?? FindByName(Path.GetFileNameWithoutExtension(app.Path))) is { } known)
                newest = Resolve(known);

            if (newest is null)
            {
                if (!File.Exists(app.Path))
                    missing++;
                continue;
            }

            if (string.Equals(newest, app.Path, StringComparison.OrdinalIgnoreCase))
                continue;

            app.Path = newest;
            ApplicationPathResolver.ConfigureVersionAwareLookup(app);
            repathed++;
        }

        return new RefreshReport(repathed, missing);
    }

    public static string? Resolve(KnownApp app)
    {
        foreach (string candidate in app.Candidates)
        {
            string? found = ResolveCandidate(Environment.ExpandEnvironmentVariables(candidate));
            if (found is not null)
                return found;
        }

        return null;
    }

    private static ProtectedApp Create(string name, string path)
    {
        var app = new ProtectedApp { Name = name, Path = path, Enabled = true };
        ApplicationPathResolver.ConfigureVersionAwareLookup(app);
        return app;
    }

    private static bool Contains(List<ProtectedApp> apps, string path) =>
        apps.Any(app => string.Equals(app.Path, path, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsName(List<ProtectedApp> apps, string name) =>
        apps.Any(app =>
            string.Equals(app.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(app.Name.Replace(" ", ""), name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(app.Path), name, StringComparison.OrdinalIgnoreCase));

    private static KnownApp? FindByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return Catalog.FirstOrDefault(known =>
            string.Equals(known.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(known.Name.Replace(" ", ""), name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NewestMatch(ProtectedApp app)
    {
        IReadOnlyList<string> paths = ApplicationPathResolver.ResolvePaths(app);
        return paths.Count == 0
            ? null
            : paths.OrderByDescending(GetVersionOrTime).First();
    }

    private static (Version ver, DateTime time) GetVersionOrTime(string path)
    {
        Version ver = new(0, 0, 0, 0);
        try
        {
            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
            if (info.FileVersion != null)
            {
                string firstToken = info.FileVersion.Split(' ', ',')[0].Trim();
                if (Version.TryParse(firstToken, out var parsed))
                    ver = parsed;
            }
        }
        catch { }

        DateTime time = SafeFileTime(path);
        return (ver, time);
    }

    private static string? ResolveCandidate(string pattern)
    {
        string root = Path.GetPathRoot(pattern) ?? string.Empty;
        if (root.Length == 0)
            return null;

        string current = root;
        string[] parts = pattern[root.Length..]
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            bool isLast = i == parts.Length - 1;

            if (part.Contains('*') || part.Contains('?'))
            {
                if (isLast)
                {
                    string? newestFile = NewestFile(current, part);
                    if (newestFile is null)
                        return null;
                    current = newestFile;
                }
                else
                {
                    string? newest = NewestDirectory(current, part);
                    if (newest is null)
                        return null;
                    current = newest;
                }
            }
            else
            {
                current = Path.Combine(current, part);
            }
        }

        return File.Exists(current) ? Path.GetFullPath(current) : null;
    }

    private static string? NewestDirectory(string parent, string wildcard)
    {
        try
        {
            return Directory.EnumerateDirectories(parent, wildcard)
                .Select(path => (Path: path, Time: SafeDirectoryTime(path)))
                .OrderByDescending(item => item.Time)
                .Select(item => item.Path)
                .FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? NewestFile(string parent, string wildcard)
    {
        try
        {
            return Directory.EnumerateFiles(parent, wildcard)
                .Select(path => (Path: path, Info: GetVersionOrTime(path)))
                .OrderByDescending(item => item.Info)
                .Select(item => item.Path)
                .FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static DateTime SafeDirectoryTime(string path)
    {
        try { return Directory.GetLastWriteTimeUtc(path); }
        catch (Exception) { return DateTime.MinValue; }
    }

    private static DateTime SafeFileTime(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); }
        catch (Exception) { return DateTime.MinValue; }
    }
}
