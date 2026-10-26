using System.Drawing;

namespace OllamaTray;

internal sealed class OllamaTrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _startItem;
    private readonly ToolStripMenuItem _stopItem;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly OllamaServerController _serverController = new();
    private readonly MemoryStream _iconStream;
    private readonly Icon _applicationIcon;
    private bool _isChecking;
    private bool _isPerformingAction;
    private bool _isOnline;

    public OllamaTrayApplicationContext()
    {
        using (var resource = typeof(Program).Assembly.GetManifestResourceStream(
                   "OllamaTray.assets.ollama.ico")
               ?? throw new InvalidOperationException("Не найден встроенный файл иконки Ollama."))
        {
            _iconStream = new MemoryStream();
            resource.CopyTo(_iconStream);
            _iconStream.Position = 0;
            _applicationIcon = new Icon(_iconStream);
        }

        _statusItem = new ToolStripMenuItem("Ollama: сервер не запущен")
        {
            Enabled = false
        };
        _startItem = new ToolStripMenuItem("Запустить Ollama", null, StartServer);
        _stopItem = new ToolStripMenuItem("Остановить Ollama", null, StopServer);
        var exitItem = new ToolStripMenuItem("Выход", null, ExitApplication);

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(
        [
            _statusItem,
            new ToolStripSeparator(),
            _startItem,
            _stopItem,
            new ToolStripSeparator(),
            exitItem
        ]);

        _trayIcon = new NotifyIcon
        {
            Icon = _applicationIcon,
            Text = "Ollama Vulkan",
            ContextMenuStrip = menu,
            Visible = true
        };

        _statusTimer = new System.Windows.Forms.Timer { Interval = 30000 };
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _statusTimer.Start();

        _ = RefreshStatusAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusTimer.Stop();
            _statusTimer.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _applicationIcon.Dispose();
            _iconStream.Dispose();
            _serverController.Dispose();
        }

        base.Dispose(disposing);
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
            ShowMessage("Не удалось проверить Ollama", exception.Message);
        }
        finally
        {
            _isChecking = false;
        }
    }

    private async void StartServer(object? sender, EventArgs e)
    {
        await PerformServerActionAsync(
            "Не удалось запустить Ollama",
            desiredState: true,
            action: _serverController.StartServerAsync);
    }

    private async void StopServer(object? sender, EventArgs e)
    {
        await PerformServerActionAsync(
            "Не удалось остановить Ollama",
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
        if (_isOnline == isOnline)
        {
            return;
        }

        _isOnline = isOnline;
        _statusItem.Text = _isOnline
            ? "Ollama: сервер запущен"
            : "Ollama: сервер не запущен";
    }

    private void ShowMessage(string title, string message)
    {
        _trayIcon.BalloonTipTitle = title;
        _trayIcon.BalloonTipText = message;
        _trayIcon.BalloonTipIcon = ToolTipIcon.Error;
        _trayIcon.ShowBalloonTip(5000);
    }

    private void ExitApplication(object? sender, EventArgs e)
    {
        ExitThread();
    }
}
