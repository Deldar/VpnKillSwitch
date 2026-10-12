using System.Text.RegularExpressions;

namespace VpnKillSwitch;

public static class ApplicationPathResolver
{
    private static readonly Regex VersionToken = new(@"\d+(?:\.\d+)+", RegexOptions.Compiled);
    private const int MaxMatches = 16;

    public static void ConfigureVersionAwareLookup(ProtectedApp app)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(app.Path);
        }
        catch (Exception)
        {
            return;
        }

        string? directory = Path.GetDirectoryName(fullPath);
        if (directory == null)
            return;

        string[] directories = directory
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        int versionIndex = Array.FindIndex(directories, segment => VersionToken.IsMatch(segment));
        if (versionIndex < 0)
            return;

        string root = Path.GetPathRoot(fullPath) ?? string.Empty;
        string beforeVersion = string.Join(Path.DirectorySeparatorChar, directories.Take(versionIndex));
        app.SearchRoot = string.IsNullOrEmpty(beforeVersion)
            ? root
            : Path.Combine(root, beforeVersion);
        app.RelativePathPatterns = directories
            .Skip(versionIndex)
            .Append(Path.GetFileName(fullPath))
            .Select(CreateSegmentPattern)
            .ToList();
        app.PathStrategy = AppPathStrategy.VersionAware;
    }

    public static IReadOnlyList<string> ResolvePaths(ProtectedApp app)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfFile(paths, app.Path);

        if (app.PathStrategy != AppPathStrategy.VersionAware ||
            string.IsNullOrWhiteSpace(app.SearchRoot) ||
            app.RelativePathPatterns.Count == 0)
        {
            return paths.ToArray();
        }

        var locations = new List<string> { app.SearchRoot };
        for (int index = 0; index < app.RelativePathPatterns.Count && locations.Count > 0; index++)
        {
            Regex pattern;
            try
            {
                pattern = new Regex(app.RelativePathPatterns[index], RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch (ArgumentException)
            {
                return paths.ToArray();
            }

            bool isFile = index == app.RelativePathPatterns.Count - 1;
            if (isFile)
            {
                foreach (string location in locations)
                {
                    foreach (string candidate in EnumerateFiles(location))
                    {
                        if (pattern.IsMatch(Path.GetFileName(candidate)))
                        {
                            AddIfFile(paths, candidate);
                            if (paths.Count >= MaxMatches)
                                return paths.ToArray();
                        }
                    }
                }

                break;
            }

            var nextLocations = new List<string>();
            foreach (string location in locations)
            {
                foreach (string candidate in EnumerateDirectories(location))
                {
                    if (pattern.IsMatch(Path.GetFileName(candidate)))
                    {
                        nextLocations.Add(candidate);
                        if (nextLocations.Count >= MaxMatches)
                            break;
                    }
                }
            }

            locations = nextLocations;
        }

        return paths.ToArray();
    }

    private static string CreateSegmentPattern(string segment)
    {
        MatchCollection versions = VersionToken.Matches(segment);
        if (versions.Count == 0)
            return "^" + Regex.Escape(segment) + "$";

        var pattern = new System.Text.StringBuilder("^");
        int position = 0;
        foreach (Match version in versions)
        {
            pattern.Append(Regex.Escape(segment[position..version.Index]));
            pattern.Append(@"\d+(?:\.\d+)+");
            position = version.Index + version.Length;
        }

        pattern.Append(Regex.Escape(segment[position..]));
        pattern.Append('$');
        return pattern.ToString();
    }

    private static void AddIfFile(ISet<string> paths, string path)
    {
        try
        {
            if (File.Exists(path))
                paths.Add(Path.GetFullPath(path));
        }
        catch (Exception)
        {
        }
    }

    private static IEnumerable<string> EnumerateDirectories(string path)
    {
        try { return Directory.EnumerateDirectories(path).ToArray(); }
        catch (Exception) { return []; }
    }

    private static IEnumerable<string> EnumerateFiles(string path)
    {
        try { return Directory.EnumerateFiles(path, "*.exe", SearchOption.TopDirectoryOnly).ToArray(); }
        catch (Exception) { return []; }
    }
}
