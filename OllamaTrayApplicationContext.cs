using System.Drawing;

namespace OllamaTray;

internal sealed class OllamaTrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _startItem;
    private readonly ToolStripMenuItem _stopItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _openLogItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly OllamaServerController _serverController;
    private readonly MemoryStream _stoppedIconStream;
    private readonly MemoryStream _runningIconStream;
    private readonly Icon _stoppedIcon;
    private readonly Icon _runningIcon;
    private bool _isChecking;
    private bool _isPerformingAction;
    private bool _isOnline;

    public OllamaTrayApplicationContext(OllamaSettings settings)
    {
        _serverController = new OllamaServerController(settings);
        _stoppedIconStream = LoadEmbeddedIcon("OllamaTray.assets.ollama-stopped.ico");
        _runningIconStream = LoadEmbeddedIcon("OllamaTray.assets.ollama-running.ico");
        _stoppedIcon = new Icon(_stoppedIconStream);
        _runningIcon = new Icon(_runningIconStream);
        _serverController.LogWriteFailed += exception =>
            ShowMessage(LocalizedText.Get("ErrorLogWriteTitle"), exception.Message);

        _statusItem = new ToolStripMenuItem(LocalizedText.Get("StatusStopped"))
        {
            Enabled = false
        };
        _statusItem.Click += CopyApiUrl;
        _startItem = new ToolStripMenuItem(LocalizedText.Get("MenuStart"), null, StartServer);
        _stopItem = new ToolStripMenuItem(LocalizedText.Get("MenuStop"), null, StopServer);
        _settingsItem = new ToolStripMenuItem(LocalizedText.Get("MenuSettings"), null, OpenSettings);
        _openLogItem = new ToolStripMenuItem(LocalizedText.Get("MenuOpenLog"), null, OpenLog);
        _exitItem = new ToolStripMenuItem(LocalizedText.Get("MenuExit"), null, ExitApplication);

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(
        [
            _statusItem,
            new ToolStripSeparator(),
            _startItem,
            _stopItem,
            new ToolStripSeparator(),
            _settingsItem,
            _openLogItem,
            _exitItem
        ]);

        _trayIcon = new NotifyIcon
        {
            Icon = _stoppedIcon,
            Text = LocalizedText.Get("TrayTitle"),
            ContextMenuStrip = menu,
            Visible = true
        };

        _statusTimer = new System.Windows.Forms.Timer { Interval = 30000 };
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _statusTimer.Start();

        _ = InitializeServerAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusTimer.Stop();
            _statusTimer.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _stoppedIcon.Dispose();
            _runningIcon.Dispose();
            _stoppedIconStream.Dispose();
            _runningIconStream.Dispose();
            _serverController.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task InitializeServerAsync()
    {
        if (_serverController.Settings.StartServerOnTrayLaunch)
        {
            await PerformServerActionAsync(
                LocalizedText.Get("ErrorStartTitle"),
                desiredState: true,
                action: _serverController.StartServerAsync);
        }
        else
        {
            await RefreshStatusAsync();
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (_isChecking || _isPerformingAction)
        {
            return;
        }

        _isChecking = true;

        try
        {
            await CheckAndUpdateStatusAsync();
        }
        catch (Exception exception)
        {
            ShowMessage(LocalizedText.Get("ErrorCheckTitle"), exception.Message);
        }
        finally
        {
            _isChecking = false;
        }
    }

    private async void StartServer(object? sender, EventArgs e)
    {
        await PerformServerActionAsync(
            LocalizedText.Get("ErrorStartTitle"),
            desiredState: true,
            action: _serverController.StartServerAsync);
    }

    private async void StopServer(object? sender, EventArgs e)
    {
        await PerformServerActionAsync(
            LocalizedText.Get("ErrorStopTitle"),
            desiredState: false,
            action: () =>
            {
                _serverController.StopServer();
                return Task.CompletedTask;
            });
    }

    private async Task PerformServerActionAsync(
        string errorTitle,
        bool desiredState,
        Func<Task> action)
    {
        if (_isPerformingAction)
        {
            return;
        }

        _isPerformingAction = true;

        try
        {
            var isOnline = await _serverController.IsServerAvailableAsync();
            UpdateStatus(isOnline);

            if (isOnline != desiredState)
            {
                await action();
                await CheckAndUpdateStatusAsync();
            }
        }
        catch (Exception exception)
        {
            ShowMessage(errorTitle, exception.Message);
        }
        finally
        {
            _isPerformingAction = false;
        }
    }

    private async Task CheckAndUpdateStatusAsync()
    {
        var isOnline = await _serverController.IsServerAvailableAsync();
        UpdateStatus(isOnline);
    }

    private void UpdateStatus(bool isOnline)
    {
        if (_isOnline != isOnline)
        {
            _isOnline = isOnline;
            _trayIcon.Icon = _isOnline ? _runningIcon : _stoppedIcon;
        }

        _statusItem.Text = _isOnline
            ? _serverController.ApiBaseUrl
            : LocalizedText.Get("StatusStopped");
        _statusItem.Enabled = _isOnline;
    }

    private void CopyApiUrl(object? sender, EventArgs e)
    {
        if (!_isOnline)
        {
            return;
        }

        try
        {
            Clipboard.SetText(_serverController.ApiBaseUrl);
        }
        catch (Exception exception)
        {
            ShowMessage(LocalizedText.Get("ErrorClipboardTitle"), exception.Message);
        }
    }

    private void OpenLog(object? sender, EventArgs e)
    {
        try
        {
            _serverController.OpenLogFile();
        }
        catch (Exception exception)
        {
            ShowMessage(LocalizedText.Get("ErrorOpenLogTitle"), exception.Message);
        }
    }

    private void OpenSettings(object? sender, EventArgs e)
    {
        try
        {
            using var settingsForm = new SettingsForm(_serverController.Settings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                _serverController.SaveSettings(settingsForm.Settings);
                LocalizedText.SetLanguage(settingsForm.Settings.Language);
                UpdateLocalizedText();
                _ = RefreshStatusAsync();
            }
        }
        catch (Exception exception)
        {
            ShowMessage(LocalizedText.Get("ErrorSettingsTitle"), exception.Message);
        }
    }

    private void UpdateLocalizedText()
    {
        _startItem.Text = LocalizedText.Get("MenuStart");
        _stopItem.Text = LocalizedText.Get("MenuStop");
        _settingsItem.Text = LocalizedText.Get("MenuSettings");
        _openLogItem.Text = LocalizedText.Get("MenuOpenLog");
        _exitItem.Text = LocalizedText.Get("MenuExit");
        _trayIcon.Text = LocalizedText.Get("TrayTitle");
        UpdateStatus(_isOnline);
    }

    private static MemoryStream LoadEmbeddedIcon(string resourceName)
    {
        using var resource = typeof(Program).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                LocalizedText.Format("ErrorIconMissing", resourceName));
        var stream = new MemoryStream();
        resource.CopyTo(stream);
        stream.Position = 0;
        return stream;
    }

    private void ShowMessage(string title, string message)
    {
        _trayIcon.BalloonTipTitle = title;
        _trayIcon.BalloonTipText = message;
        _trayIcon.BalloonTipIcon = ToolTipIcon.Error;
        _trayIcon.ShowBalloonTip(5000);
    }

    private async void ExitApplication(object? sender, EventArgs e)
    {
        if (_isPerformingAction)
        {
            return;
        }

        if (_serverController.Settings.StopServerOnTrayExit)
        {
            _isPerformingAction = true;
            try
            {
                if (await _serverController.IsServerAvailableAsync())
                {
                    _serverController.StopServer();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    LocalizedText.Format("ErrorStopOnExit", exception.Message),
                    LocalizedText.Get("ErrorStopTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isPerformingAction = false;
            }
        }

        ExitThread();
    }
}
