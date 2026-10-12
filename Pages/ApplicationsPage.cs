namespace VpnKillSwitch;

internal sealed class ApplicationsPage : PagePanel
{
    private readonly AppSettings _settings;
    private readonly ProtectedAppsList _list = new() { Dock = DockStyle.Fill };
    private readonly NoticeBar _notice = new() { Dock = DockStyle.Top };
    private readonly System.Windows.Forms.Timer _noticeTimer;
    private readonly FluentButton _refresh;
    private readonly FluentButton _detect;

    public ApplicationsPage(AppSettings settings)
    {
        _settings = settings;

        var header = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.Transparent };
        var title = new PageHeader(
            "Protected applications",
            "These applications stay offline unless the secure VPN tunnel is healthy.") { Dock = DockStyle.Fill };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 660,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 17, 0, 0)
        };
        _refresh = Button("Refresh paths", FluentButtonStyle.Subtle, 120, OnRefresh);
        _detect = Button("Detect installed", FluentButtonStyle.Subtle, 130, OnDetectInstalled);
        actions.Controls.Add(_refresh);
        actions.Controls.Add(_detect);
        actions.Controls.Add(Button("Remove selected", FluentButtonStyle.Subtle, 136, OnRemoveSelected));
        actions.Controls.Add(Button("Add path", FluentButtonStyle.Subtle, 96, OnAddPathManual));
        actions.Controls.Add(Button("Add application", FluentButtonStyle.Accent, 140, OnAddApplication));

        header.Controls.Add(title);
        header.Controls.Add(actions);

        _notice.VisibleChanged += (_, _) => PerformLayout();
        PageLayout.Compose(this, _list, [header, _notice], []);

        _noticeTimer = new System.Windows.Forms.Timer { Interval = 8000 };
        _noticeTimer.Tick += (_, _) => { _noticeTimer.Stop(); _notice.Hide(); };
        Disposed += (_, _) => _noticeTimer.Dispose();

        _list.AppChanged += (_, _) => _settings.Save();

        AllowDrop = true;
        _list.AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        _list.DragEnter += OnDragEnter;
        _list.DragDrop += OnDragDrop;

        Reload();
        AutoRefreshOnLoad();
    }

    public void Reload()
    {
        _list.ClearApps();
        foreach (ProtectedApp app in _settings.ProtectedApps)
        {
            if (string.IsNullOrWhiteSpace(app.Id))
                app.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(app.Name))
                app.Name = Path.GetFileNameWithoutExtension(app.Path);
            if (app.PathStrategy == AppPathStrategy.Exact)
                ApplicationPathResolver.ConfigureVersionAwareLookup(app);
            _list.AddApp(app);
        }
    }

    public void ShowNotice(string message, NoticeKind kind = NoticeKind.Warning)
    {
        _notice.Show(message, kind);
        _noticeTimer.Stop();
        _noticeTimer.Start();
    }

    private async void AutoRefreshOnLoad()
    {
        try
        {
            RefreshReport report = await Task.Run(() => KnownApps.Refresh(_settings.ProtectedApps));
            var (added, names) = await Task.Run(() => KnownApps.ScanAndAdd(_settings.ProtectedApps));
            if (report.Repathed > 0 || added > 0)
            {
                _settings.Save();
                Reload();
                if (added > 0 && report.Repathed > 0)
                    ShowNotice($"Auto-detected {added} installed tool(s) and refreshed {report.Repathed} path(s).", NoticeKind.Success);
                else if (added > 0)
                    ShowNotice($"Auto-detected {added} installed tool(s): {string.Join(", ", names)}.", NoticeKind.Success);
                else
                    ShowNotice(report.Describe(), NoticeKind.Success);
            }
        }
        catch
        {
        }
    }

    private async void OnDetectInstalled(object? sender, EventArgs e)
    {
        _detect.SetBusy(true, "Scanning\u2026");
        try
        {
            var (added, names) = await Task.Run(() => KnownApps.ScanAndAdd(_settings.ProtectedApps));
            if (added > 0)
            {
                _settings.Save();
                Reload();
                string listStr = string.Join(", ", names.Take(3)) + (names.Count > 3 ? $" and {names.Count - 3} more" : "");
                ShowNotice($"Detected and added {added} installed tool(s): {listStr}.", NoticeKind.Success);
            }
            else
            {
                ShowNotice("All installed well-known IDEs and AI tools are already in the list.", NoticeKind.Success);
            }
        }
        catch (Exception ex)
        {
            ShowNotice(ex.Message, NoticeKind.Failure);
        }
        finally
        {
            _detect.SetBusy(false);
        }
    }

    private async void OnRefresh(object? sender, EventArgs e)
    {
        _refresh.SetBusy(true, "Refreshing\u2026");
        try
        {
            List<ProtectedApp> apps = _settings.ProtectedApps;
            RefreshReport report = await Task.Run(() => KnownApps.Refresh(apps));

            _settings.Save();
            Reload();
            ShowNotice(report.Describe(), report.Missing > 0 ? NoticeKind.Warning : NoticeKind.Success);
        }
        catch (Exception ex)
        {
            ShowNotice(ex.Message, NoticeKind.Failure);
        }
        finally
        {
            _refresh.SetBusy(false);
        }
    }

    private void OnAddApplication(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Applications (*.exe)|*.exe|Shortcuts (*.lnk)|*.lnk|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true,
            Title = "Select applications to protect"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        int added = 0;
        int updated = 0;
        foreach (string path in dialog.FileNames)
        {
            var res = AddOrUpdatePath(path);
            if (res == AddResult.Added) added++;
            else if (res == AddResult.Updated) updated++;
        }

        if (added == 0 && updated == 0)
        {
            ShowNotice("The selected application(s) are already protected.", NoticeKind.Warning);
            return;
        }

        _settings.Save();
        Reload();
        ShowNotice(FormatNotice(added, updated), NoticeKind.Success);
    }

    private void OnAddPathManual(object? sender, EventArgs e)
    {
        using var dialog = new PathInputDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var res = AddOrUpdatePath(dialog.EnteredPath);
        if (res == AddResult.None)
        {
            string expanded = Environment.ExpandEnvironmentVariables(dialog.EnteredPath);
            if (!File.Exists(expanded))
                ShowNotice("File not found at the specified path.", NoticeKind.Failure);
            else
                ShowNotice("This application is already protected.", NoticeKind.Warning);
            return;
        }

        _settings.Save();
        Reload();
        ShowNotice(FormatNotice(res == AddResult.Added ? 1 : 0, res == AddResult.Updated ? 1 : 0), NoticeKind.Success);
    }

    private void OnRemoveSelected(object? sender, EventArgs e)
    {
        _list.RemoveSelected();
        _settings.ProtectedApps = _list.GetApps().ToList();
        _settings.Save();
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true ||
            e.Data?.GetDataPresent(DataFormats.Text) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
        {
            int added = 0, updated = 0;
            foreach (string file in files)
            {
                var res = AddOrUpdatePath(file);
                if (res == AddResult.Added) added++;
                else if (res == AddResult.Updated) updated++;
            }

            if (added > 0 || updated > 0)
            {
                _settings.Save();
                Reload();
                ShowNotice(FormatNotice(added, updated), NoticeKind.Success);
            }
        }
        else if (e.Data?.GetData(DataFormats.Text) is string text)
        {
            var res = AddOrUpdatePath(text.Trim().Trim('"'));
            if (res != AddResult.None)
            {
                _settings.Save();
                Reload();
                ShowNotice(FormatNotice(res == AddResult.Added ? 1 : 0, res == AddResult.Updated ? 1 : 0), NoticeKind.Success);
            }
        }
    }

    private enum AddResult { None, Added, Updated }

    private AddResult AddOrUpdatePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return AddResult.None;

        string target = ResolveShortcutOrExe(rawPath.Trim().Trim('"'));
        string path;
        try
        {
            string expanded = Environment.ExpandEnvironmentVariables(target);
            path = Path.GetFullPath(expanded);
        }
        catch
        {
            return AddResult.None;
        }

        if (!File.Exists(path))
            return AddResult.None;

        var exactMatch = _settings.ProtectedApps.FirstOrDefault(a =>
            string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return AddResult.None;

        string exeName = Path.GetFileNameWithoutExtension(path);

        var existingSameApp = _settings.ProtectedApps.FirstOrDefault(a =>
            string.Equals(a.Name, exeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(a.Path), exeName, StringComparison.OrdinalIgnoreCase));

        if (existingSameApp != null)
        {
            existingSameApp.Path = path;
            ApplicationPathResolver.ConfigureVersionAwareLookup(existingSameApp);
            return AddResult.Updated;
        }

        var app = ProtectedApp.FromPath(path);
        _settings.ProtectedApps.Add(app);
        return AddResult.Added;
    }

    private static string ResolveShortcutOrExe(string path)
    {
        if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            return path;

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(path);
                string target = (string)shortcut.TargetPath;
                if (!string.IsNullOrWhiteSpace(target) && File.Exists(target))
                    return target;
            }
        }
        catch
        {
        }

        return path;
    }

    private static string FormatNotice(int added, int updated) => (added, updated) switch
    {
        ( > 0, > 0) => $"Added {added} and updated {updated} application(s).",
        ( > 0, 0) => $"Added {added} application(s).",
        (0, > 0) => $"Updated {updated} application(s) to newest path.",
        _ => "Protected applications updated."
    };
}

internal sealed class PathInputDialog : Form
{
    private readonly TextBox _textBox;

    public string EnteredPath => _textBox.Text.Trim().Trim('"');

    public PathInputDialog()
    {
        Text = "Add application path";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(540, 220);
        BackColor = UiTheme.Layer;
        ForeColor = UiTheme.Text;
        Font = UiTheme.Body;

        var label = new Label
        {
            Text = "Enter or paste the path to an executable (.exe) or shortcut (.lnk):",
            Location = new Point(24, 20),
            AutoSize = true,
            ForeColor = UiTheme.TextSecondary
        };

        var textCard = new RoundedSurface
        {
            Location = new Point(24, 52),
            Size = new Size(476, 44),
            Fill = UiTheme.Input,
            BorderColor = UiTheme.StrokeStrong,
            Radius = 8
        };

        _textBox = new TextBox
        {
            Location = new Point(12, 11),
            Width = 452,
            BorderStyle = BorderStyle.None,
            BackColor = UiTheme.Input,
            ForeColor = UiTheme.Text,
            Font = UiTheme.Body
        };
        textCard.Controls.Add(_textBox);

        try
        {
            if (Clipboard.ContainsText())
            {
                string clip = Clipboard.GetText().Trim().Trim('"');
                if (clip.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    clip.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
                    File.Exists(Environment.ExpandEnvironmentVariables(clip)))
                {
                    _textBox.Text = clip;
                    _textBox.SelectAll();
                }
            }
        }
        catch
        {
        }

        void Submit()
        {
            if (string.IsNullOrWhiteSpace(EnteredPath))
            {
                MessageBox.Show(this, "Please enter an application path.", "VPN Kill Switch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        var btnAdd = new FluentButton
        {
            Text = "Add",
            Kind = FluentButtonStyle.Accent,
            Location = new Point(380, 116),
            Size = new Size(120, 36)
        };
        btnAdd.Click += (_, _) => Submit();

        var btnCancel = new FluentButton
        {
            Text = "Cancel",
            Kind = FluentButtonStyle.Subtle,
            Location = new Point(252, 116),
            Size = new Size(120, 36)
        };
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(label);
        Controls.Add(textCard);
        Controls.Add(btnAdd);
        Controls.Add(btnCancel);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Submit();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
    }
}
