using System.Text;

namespace VpnKillSwitch;

internal static class Diagnostics
{
    private const long MaxBytes = 512 * 1024;
    private const int MaxFiles = 3;

    private static readonly object Gate = new();
    private static string _path = string.Empty;

    public static string LogPath
    {
        get
        {
            if (_path.Length == 0)
                _path = Path.Combine(AppContext.BaseDirectory, "vpnkillswitch.log");

            return _path;
        }
    }

    public static void Write(string area, string message)
    {
        string line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{area}] {message}";

        lock (Gate)
        {
            try
            {
                Rotate();
                File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception)
            {
            }
        }
    }

    public static void WriteState(string area, string message) => Write(area, message);

    public static string Tail(int lines)
    {
        lock (Gate)
        {
            try
            {
                if (!File.Exists(LogPath))
                    return string.Empty;

                string[] all = File.ReadAllLines(LogPath);
                int take = Math.Min(lines, all.Length);
                return string.Join(Environment.NewLine, all.Skip(all.Length - take));
            }
            catch (Exception ex)
            {
                return $"Could not read the log: {ex.Message}";
            }
        }
    }

    private static void Rotate()
    {
        if (!File.Exists(LogPath))
            return;

        var info = new FileInfo(LogPath);
        if (info.Length < MaxBytes)
            return;

        for (int i = MaxFiles - 1; i >= 1; i--)
        {
            string older = $"{LogPath}.{i}";
            string newer = $"{LogPath}.{i + 1}";

            if (!File.Exists(older))
                continue;

            if (File.Exists(newer))
                File.Delete(newer);

            File.Move(older, newer);
        }

        string first = $"{LogPath}.1";
        if (File.Exists(first))
            File.Delete(first);

        File.Move(LogPath, first);
    }
}
