using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace VpnKillSwitch;

public enum AppTheme
{
    Dark,
    Light
}

internal sealed class ThemePalette
{
    public Color Layer;
    public Color Input;
    public Color Stroke;
    public Color StrokeStrong;

    public Color Text;
    public Color TextSecondary;
    public Color TextTertiary;

    public Color Accent;
    public Color AccentFill;
    public Color AccentFillHover;
    public Color AccentFillPressed;

    public Color Success;
    public Color Caution;
    public Color Critical;
    public Color CriticalFill;


    public Color ToggleOffDisc;
    public Color ToggleOffGlyph;
    public Color ToggleOnDisc;
    public Color ToggleBlockedDisc;
    public Color ToggleWarnDisc;

    public Color AuroraBaseA;
    public Color AuroraBaseB;
    public Color AuroraA;
    public Color AuroraB;
    public Color AuroraC;
    public Color AlertA;
    public Color AlertB;
    public Color AlertC;
    public Color AuroraVeil;
    public Color RegionSidebar;
    public Color RegionCaption;
    public Color GlassFill;
    public Color GlassFillStrong;
    public Color GlassBorder;
    public Color GlassHover;
    public Color PillA;
    public Color PillB;
}

internal readonly record struct StatusVisual(
    Color Accent,
    Color Disc,
    Color Glyph,
    string Headline,
    string Subtitle,
    string SidebarLine1,
    string SidebarLine2,
    TrayVisualState Tray);

internal static class UiTheme
{
    private static readonly ThemePalette DarkPalette = new()
    {
        Layer = Color.FromArgb(44, 44, 44),
        Input = Color.FromArgb(32, 32, 32),
        Stroke = Color.FromArgb(62, 62, 62),
        StrokeStrong = Color.FromArgb(90, 90, 90),

        Text = Color.FromArgb(255, 255, 255),
        TextSecondary = Color.FromArgb(200, 200, 200),
        TextTertiary = Color.FromArgb(154, 154, 154),

        Accent = Color.FromArgb(96, 205, 255),
        AccentFill = Color.FromArgb(0, 120, 212),
        AccentFillHover = Color.FromArgb(26, 137, 223),
        AccentFillPressed = Color.FromArgb(0, 103, 192),

        Success = Color.FromArgb(108, 203, 95),
        Caution = Color.FromArgb(249, 191, 70),
        Critical = Color.FromArgb(255, 153, 164),
        CriticalFill = Color.FromArgb(196, 43, 28),


        ToggleOffDisc = Color.FromArgb(38, 38, 38),
        ToggleOffGlyph = Color.FromArgb(154, 154, 154),
        ToggleOnDisc = Color.FromArgb(24, 92, 58),
        ToggleBlockedDisc = Color.FromArgb(120, 32, 26),
        ToggleWarnDisc = Color.FromArgb(96, 74, 20),

        AuroraBaseA = Color.FromArgb(15, 17, 32),
        AuroraBaseB = Color.FromArgb(8, 9, 16),
        AuroraA = Color.FromArgb(74, 150, 255),
        AuroraB = Color.FromArgb(120, 108, 255),
        AuroraC = Color.FromArgb(60, 200, 200),
        AlertA = Color.FromArgb(255, 86, 96),
        AlertB = Color.FromArgb(255, 150, 80),
        AlertC = Color.FromArgb(150, 40, 120),
        AuroraVeil = Color.FromArgb(92, 8, 10, 18),
        RegionSidebar = Color.FromArgb(16, 255, 255, 255),
        RegionCaption = Color.FromArgb(52, 0, 0, 0),
        GlassFill = Color.FromArgb(22, 255, 255, 255),
        GlassFillStrong = Color.FromArgb(34, 255, 255, 255),
        GlassBorder = Color.FromArgb(44, 255, 255, 255),
        GlassHover = Color.FromArgb(32, 255, 255, 255),
        PillA = Color.FromArgb(84, 170, 255),
        PillB = Color.FromArgb(20, 100, 210)
    };

    private static readonly ThemePalette LightPalette = new()
    {
        Layer = Color.FromArgb(255, 255, 255),
        Input = Color.FromArgb(255, 255, 255),
        Stroke = Color.FromArgb(225, 225, 225),
        StrokeStrong = Color.FromArgb(198, 198, 198),

        Text = Color.FromArgb(27, 27, 27),
        TextSecondary = Color.FromArgb(90, 90, 90),
        TextTertiary = Color.FromArgb(138, 138, 138),

        Accent = Color.FromArgb(0, 95, 184),
        AccentFill = Color.FromArgb(0, 103, 192),
        AccentFillHover = Color.FromArgb(16, 110, 190),
        AccentFillPressed = Color.FromArgb(0, 90, 158),

        Success = Color.FromArgb(15, 123, 15),
        Caution = Color.FromArgb(157, 93, 0),
        Critical = Color.FromArgb(196, 43, 28),
        CriticalFill = Color.FromArgb(196, 43, 28),


        ToggleOffDisc = Color.FromArgb(232, 232, 232),
        ToggleOffGlyph = Color.FromArgb(120, 120, 120),
        ToggleOnDisc = Color.FromArgb(21, 109, 60),
        ToggleBlockedDisc = Color.FromArgb(170, 40, 30),
        ToggleWarnDisc = Color.FromArgb(150, 110, 20),

        AuroraBaseA = Color.FromArgb(232, 238, 250),
        AuroraBaseB = Color.FromArgb(244, 246, 252),
        AuroraA = Color.FromArgb(130, 180, 255),
        AuroraB = Color.FromArgb(178, 170, 255),
        AuroraC = Color.FromArgb(150, 225, 235),
        AlertA = Color.FromArgb(255, 150, 150),
        AlertB = Color.FromArgb(255, 190, 140),
        AlertC = Color.FromArgb(220, 150, 220),
        AuroraVeil = Color.FromArgb(120, 255, 255, 255),
        RegionSidebar = Color.FromArgb(86, 255, 255, 255),
        RegionCaption = Color.FromArgb(120, 255, 255, 255),
        GlassFill = Color.FromArgb(150, 255, 255, 255),
        GlassFillStrong = Color.FromArgb(205, 255, 255, 255),
        GlassBorder = Color.FromArgb(215, 255, 255, 255),
        GlassHover = Color.FromArgb(185, 255, 255, 255),
        PillA = Color.FromArgb(84, 170, 255),
        PillB = Color.FromArgb(20, 100, 210)
    };

    public static AppTheme Current { get; private set; } = AppTheme.Dark;

    public static ThemePalette P { get; private set; } = DarkPalette;

    public static bool IsDark => Current == AppTheme.Dark;

    public static event Action? Changed;

    public static void Apply(AppTheme theme)
    {
        Current = theme;
        P = theme == AppTheme.Light ? LightPalette : DarkPalette;
        Changed?.Invoke();
    }

    public static void Bind(Control control, Action apply)
    {
        apply();

        void OnChanged()
        {
            if (control.IsDisposed)
            {
                Changed -= OnChanged;
                return;
            }

            apply();
            control.Invalidate(true);
        }

        Changed += OnChanged;
        control.Disposed += (_, _) => Changed -= OnChanged;
    }

    public static Color Layer => P.Layer;

    public static Color Input => P.Input;

    public static Color Stroke => P.Stroke;

    public static Color StrokeStrong => P.StrokeStrong;

    public static Color Text => P.Text;

    public static Color TextSecondary => P.TextSecondary;

    public static Color TextTertiary => P.TextTertiary;

    public static Color Accent => P.Accent;

    public static Color AccentFill => P.AccentFill;

    public static Color AccentFillHover => P.AccentFillHover;

    public static Color AccentFillPressed => P.AccentFillPressed;

    public static Color Success => P.Success;

    public static Color Caution => P.Caution;

    public static Color Critical => P.Critical;

    public static Color CriticalFill => P.CriticalFill;

    public static Color AuroraBaseA => P.AuroraBaseA;

    public static Color AuroraBaseB => P.AuroraBaseB;

    public static Color AuroraA => P.AuroraA;

    public static Color AuroraB => P.AuroraB;

    public static Color AuroraC => P.AuroraC;

    public static Color AlertA => P.AlertA;

    public static Color AlertB => P.AlertB;

    public static Color AlertC => P.AlertC;

    public static Color AuroraVeil => P.AuroraVeil;

    public static Color RegionSidebar => P.RegionSidebar;

    public static Color RegionCaption => P.RegionCaption;

    public static Color GlassFill => P.GlassFill;

    public static Color GlassFillStrong => P.GlassFillStrong;

    public static Color GlassBorder => P.GlassBorder;

    public static Color GlassHover => P.GlassHover;

    public static Color PillA => P.PillA;

    public static Color PillB => P.PillB;

    public static StatusVisual ForGuard(GuardStatus status)
    {
        int apps = status.ProtectedApps.Count;
        string guarded = apps == 0 ? "No apps guarded" : $"{apps} apps guarded";

        if (!status.Armed)
        {
            return new StatusVisual(
                TextTertiary,
                P.ToggleOffDisc,
                P.ToggleOffGlyph,
                "OFF",
                "Protection is off \u2014 press to start",
                "Off",
                guarded,
                TrayVisualState.Idle);
        }

        return status.Verdict switch
        {
            GuardVerdict.Leaking => new StatusVisual(
                Critical,
                P.ToggleBlockedDisc,
                Color.White,
                "LEAKING",
                "Traffic is routing outside the tunnel \u2014 apps are blocked",
                "Leaking",
                status.BlockedCount == 0 ? "Blocking failed" : $"{status.BlockedCount} apps blocked",
                TrayVisualState.Blocked),

            GuardVerdict.NotConfigured => new StatusVisual(
                Caution,
                P.ToggleWarnDisc,
                Color.White,
                "NOT SET UP",
                "Add a tunnel signal on the Tunnel page",
                "Not set up",
                "Nothing to detect with",
                TrayVisualState.Connecting),

            GuardVerdict.Unknown => new StatusVisual(
                Caution,
                P.ToggleWarnDisc,
                Color.White,
                "UNCONFIRMED",
                "The tunnel could not be confirmed, so apps stay blocked",
                "Unconfirmed",
                status.BlockedCount == 0 ? "Blocking failed" : $"{status.BlockedCount} apps blocked",
                TrayVisualState.Connecting),

            _ => status.Blocking
                ? new StatusVisual(
                    Caution,
                    P.ToggleWarnDisc,
                    Color.White,
                    "RESTORING",
                    $"Tunnel looks healthy again \u2014 verifying stability ({status.SafeStreak} safe so far)",
                    "Restoring",
                    status.SafeStreak == 1 ? "1 safe check so far" : $"{status.SafeStreak} safe checks so far",
                    TrayVisualState.Connecting)
                : new StatusVisual(
                    Success,
                    P.ToggleOnDisc,
                    Color.White,
                    "PROTECTED",
                    "Traffic is going through the tunnel",
                    "Protected",
                    guarded,
                    TrayVisualState.Connected)
        };
    }

    public static SignalReading? Reading(GuardStatus status, string name)
    {
        foreach (SignalReading reading in status.Readings)
        {
            if (reading.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                return reading;
        }

        return null;
    }

    public static int HealthySignalCount(GuardStatus status) =>
        status.Readings.Count(reading => reading.State == SignalState.Healthy);

    public static bool IsTransient(GuardStatus status) =>
        status.Armed && status.Verdict is GuardVerdict.Unknown or GuardVerdict.NotConfigured;

    public static readonly Font CaptionTitle = CreateFont(15.5f, FontStyle.Bold);
    public static readonly Font Section = CreateFont(12f, FontStyle.Bold);
    public static readonly Font Body = CreateFont(9.5f, FontStyle.Regular);
    public static readonly Font BodyStrong = CreateFont(9.5f, FontStyle.Bold);
    public static readonly Font CaptionText = CreateFont(8.5f, FontStyle.Regular);
    public static readonly Font Hero = CreateFont(18f, FontStyle.Bold);
    public static readonly Font Nav = CreateFont(10.5f, FontStyle.Regular);
    public static readonly Font NavStrong = CreateFont(10.5f, FontStyle.Bold);
    public static readonly Font Glyph = CreateIconFont(10f);

    public static Font CreateFont(float size, FontStyle style)
    {
        string[] families = ["Segoe UI Variable Display", "Segoe UI Variable Text", "Segoe UI"];
        foreach (string family in families)
        {
            try
            {
                return new Font(family, size, style, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
            }
        }

        return new Font(SystemFonts.MessageBoxFont!.FontFamily, size, style, GraphicsUnit.Point);
    }

    public static Font CreateIconFont(float size)
    {
        string[] families = ["Segoe Fluent Icons", "Segoe MDL2 Assets", "Segoe UI Symbol"];
        foreach (string family in families)
        {
            try
            {
                return new Font(family, size, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
            }
        }

        return Body;
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(1, radius * 2);
        Rectangle rect = bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            path.AddRectangle(new Rectangle(bounds.X, bounds.Y, Math.Max(1, bounds.Width), Math.Max(1, bounds.Height)));
            return path;
        }

        rect.Width = Math.Max(1, rect.Width - 1);
        rect.Height = Math.Max(1, rect.Height - 1);
        int arc = Math.Min(d, Math.Min(rect.Width, rect.Height));
        path.AddArc(rect.X, rect.Y, arc, arc, 180, 90);
        path.AddArc(rect.Right - arc, rect.Y, arc, arc, 270, 90);
        path.AddArc(rect.Right - arc, rect.Bottom - arc, arc, arc, 0, 90);
        path.AddArc(rect.X, rect.Bottom - arc, arc, arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void ConfigureGraphics(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    public static Color Mix(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            from.A,
            (int)Math.Round(from.R + (to.R - from.R) * amount),
            (int)Math.Round(from.G + (to.G - from.G) * amount),
            (int)Math.Round(from.B + (to.B - from.B) * amount));
    }

    public static Color Alpha(Color color, int alpha) => Color.FromArgb(Math.Clamp(alpha, 0, 255), color);

    public static void ApplyNativeTheme(Control control)
    {
        if (control.IsDisposed || !control.IsHandleCreated)
            return;

        try
        {
            SetWindowTheme(control.Handle, IsDark ? "DarkMode_Explorer" : "Explorer", null);
        }
        catch (Exception)
        {
        }
    }

    public static void ApplyNativeThemes(Control root)
    {
        if (root.IsDisposed)
            return;

        ApplyNativeTheme(root);
        foreach (Control child in root.Controls)
            ApplyNativeThemes(child);
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);
}

internal static class WindowChrome
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwcpRound = 2;
    private const int DwmwcpDoNotRound = 1;

    public static void Apply(Form form, bool rounded)
    {
        if (!form.IsHandleCreated)
            return;

        int dark = UiTheme.IsDark ? 1 : 0;
        _ = DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));

        int corner = rounded ? DwmwcpRound : DwmwcpDoNotRound;
        _ = DwmSetWindowAttribute(form.Handle, DwmwaWindowCornerPreference, ref corner, sizeof(int));

        int border = ColorTranslator.ToWin32(UiTheme.Stroke);
        _ = DwmSetWindowAttribute(form.Handle, DwmwaBorderColor, ref border, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
