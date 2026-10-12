using System.Drawing.Drawing2D;

namespace VpnKillSwitch;

public enum TrayVisualState
{
    Idle,
    Connecting,
    Connected,
    Blocked
}

public sealed class TrayIconSet : IDisposable
{
    public static readonly Color IdleAccent = Color.FromArgb(154, 154, 154);
    public static readonly Color ConnectingAccent = Color.FromArgb(249, 191, 70);
    public static readonly Color ConnectedAccent = Color.FromArgb(108, 203, 95);
    public static readonly Color BlockedAccent = Color.FromArgb(255, 120, 120);

    private readonly Icon _idle;
    private readonly Icon _connecting;
    private readonly Icon _connected;
    private readonly Icon _blocked;

    public TrayIconSet()
    {
        _idle = CreateIcon(IdleAccent, Color.FromArgb(58, 58, 58));
        _connecting = CreateIcon(ConnectingAccent, Color.FromArgb(92, 70, 22));
        _connected = CreateIcon(ConnectedAccent, Color.FromArgb(22, 92, 58));
        _blocked = CreateIcon(BlockedAccent, Color.FromArgb(110, 32, 26));
    }

    public Icon GetIcon(TrayVisualState state) => state switch
    {
        TrayVisualState.Connecting => _connecting,
        TrayVisualState.Connected => _connected,
        TrayVisualState.Blocked => _blocked,
        _ => _idle
    };

    public void Dispose()
    {
        _idle.Dispose();
        _connecting.Dispose();
        _connected.Dispose();
        _blocked.Dispose();
    }

    private static Icon CreateIcon(Color accent, Color fill)
    {
        using var bitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var body = new SolidBrush(fill);
            using var path = new GraphicsPath();
            path.AddArc(4, 4, 10, 10, 180, 90);
            path.AddArc(18, 4, 10, 10, 270, 90);
            path.AddArc(18, 18, 10, 10, 0, 90);
            path.AddArc(4, 18, 10, 10, 90, 90);
            path.CloseFigure();
            g.FillPath(body, path);

            using var ring = new Pen(accent, 2.2f);
            g.DrawPath(ring, path);

            using var shield = new SolidBrush(accent);
            Point[] points =
            [
                new Point(16, 8),
                new Point(22, 11),
                new Point(22, 18),
                new Point(16, 24),
                new Point(10, 18),
                new Point(10, 11)
            ];
            g.FillPolygon(shield, points);
        }

        IntPtr handle = bitmap.GetHicon();
        Icon created = Icon.FromHandle(handle);
        Icon clone = (Icon)created.Clone();
        DestroyIcon(handle);
        return clone;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
