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
    private bool _isChecking;
    private bool _isPerformingAction;
    private bool _isOnline;

    public OllamaTrayApplicationContext()
    {
        _statusItem = new ToolStripMenuItem("Ollama: проверка статуса...")
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
            Icon = SystemIcons.Application,
            Text = "Ollama Vulkan",
            ContextMenuStrip = menu,
            Visible = true
        };

        _statusTimer = new System.Windows.Forms.Timer { Interval = 3000 };
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
        _statusItem.Text = "Ollama: проверка статуса...";
        UpdateMenuItems();

        try
        {
            _isOnline = await _serverController.IsServerAvailableAsync();
            _statusItem.Text = _isOnline
                ? "Ollama: сервер запущен"
                : "Ollama: сервер не отвечает";
        }
        catch (Exception exception)
        {
            _isOnline = false;
            _statusItem.Text = "Ollama: ошибка проверки";
            ShowMessage("Не удалось проверить Ollama", exception.Message);
        }
        finally
        {
            _isChecking = false;
            UpdateMenuItems();
        }
    }

    private async void StartServer(object? sender, EventArgs e)
    {
        await PerformServerActionAsync(
            "Не удалось запустить Ollama",
            _serverController.StartServer);
    }

    private async void StopServer(object? sender, EventArgs e)
    {
        await PerformServerActionAsync(
            "Не удалось остановить Ollama",
            _serverController.StopServer);
    }

    private async Task PerformServerActionAsync(string errorTitle, Action action)
    {
        if (_isPerformingAction)
        {
            return;
        }

        _isPerformingAction = true;
        _statusItem.Text = "Ollama: выполняется команда...";
        UpdateMenuItems();

        try
        {
            action();
        }
        catch (Exception exception)
        {
            ShowMessage(errorTitle, exception.Message);
        }
        finally
        {
            _isPerformingAction = false;
            await RefreshStatusAsync();
        }
    }

    private void UpdateMenuItems()
    {
        _startItem.Enabled = !_isOnline && !_isPerformingAction && !_isChecking;
        _stopItem.Enabled = !_isPerformingAction && !_isChecking;
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
