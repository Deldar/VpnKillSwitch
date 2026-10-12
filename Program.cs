namespace VpnKillSwitch;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);

        if (!OperatingSystem.IsWindows())
        {
            MessageBox.Show("This application only runs on Windows.", "VPN Kill Switch");
            return;
        }

        AppSettings settings = AppSettings.Load();
        UiTheme.Apply(settings.Theme);
        settings.AutoConfigureTunnel();

        if (!settings.KnownAppsSeeded)
        {
            KnownApps.Seed(settings.ProtectedApps);
            settings.KnownAppsSeeded = true;
        }

        try
        {
            var (added, names) = KnownApps.ScanAndAdd(settings.ProtectedApps);
            if (added > 0)
                Diagnostics.Write("app", $"auto-detected {added} new installed app(s) on startup: {string.Join(", ", names)}");

            RefreshReport report = KnownApps.Refresh(settings.ProtectedApps);
            if (report.Repathed > 0)
                Diagnostics.Write("app", $"auto-updated paths on startup: {report.Describe()}");
        }
        catch (Exception ex)
        {
            Diagnostics.Write("app", $"startup app scan/refresh error: {ex.Message}");
        }

        settings.Save();
        Diagnostics.Write("app", $"starting; settings at {AppSettings.FilePath}");

        var guard = new GuardController(settings);
        guard.RecoverOrphanedRules();

        Application.Run(new TrayApplicationContext(guard));
    }
}
