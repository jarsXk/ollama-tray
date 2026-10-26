using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace OllamaTray;

internal sealed class OllamaServerController : IDisposable
{
    private static readonly IPAddress OllamaAddress = IPAddress.Loopback;
    private static readonly IPAddress AnyAddress = IPAddress.Any;
    private const int OllamaPort = 11434;
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri($"http://{OllamaAddress}:{OllamaPort}/"),
        Timeout = TimeSpan.FromSeconds(2)
    };

    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/version");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task StartServerAsync()
    {
        var startupScript = FindStartupScript();
        var startInfo = startupScript is null
            ? CreatePathStartupInfo()
            : CreateStartupInfo(startupScript);
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Не удалось запустить процесс cmd.exe.");

        var timeout = TimeSpan.FromSeconds(15);
        var startedAt = Stopwatch.StartNew();
        while (startedAt.Elapsed < timeout)
        {
            if (await IsServerAvailableAsync())
            {
                return;
            }

            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Команда запуска Ollama завершилась с кодом {process.ExitCode}. " +
                    GetStartupLocationsMessage(startupScript));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            $"Ollama не ответила в течение {timeout.TotalSeconds:0} секунд. " +
            GetStartupLocationsMessage(startupScript));
    }

    private static ProcessStartInfo CreateStartupInfo(string startupScript)
    {
        return new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/d /c \"\"{startupScript}\" /hidden\"",
            WorkingDirectory = Path.GetDirectoryName(startupScript)!,
            CreateNoWindow = true,
            UseShellExecute = false
        };
    }

    private static ProcessStartInfo CreatePathStartupInfo()
    {
        return new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/d /c start-ollama.cmd /hidden",
            CreateNoWindow = true,
            UseShellExecute = false
        };
    }

    private static string GetStartupLocationsMessage(string? startupScript)
    {
        var checkedLocations = startupScript is null
            ? "Проверены соседняя папка ollama-vulkan и стандартная папка установки " +
              "%LOCALAPPDATA%\\Programs\\Ollama Vulkan; затем выполнена попытка запуска через PATH."
            : $"Использовался скрипт \"{startupScript}\".";

        return checkedLocations + " Проверьте наличие start-ollama.cmd и настройки PATH.";
    }

    public void StopServer()
    {
        var processIds = WindowsTcpTable.GetListeningProcessIds(
            OllamaPort,
            OllamaAddress,
            AnyAddress);
        var ollamaProcesses = new List<Process>();

        foreach (var processId in processIds)
        {
            var process = Process.GetProcessById(processId);
            try
            {
                if (string.Equals(
                    Path.GetFileName(process.MainModule?.FileName),
                    "ollama.exe",
                    StringComparison.OrdinalIgnoreCase))
                {
                    ollamaProcesses.Add(process);
                }
                else
                {
                    process.Dispose();
                }
            }
            catch
            {
                process.Dispose();
                throw;
            }
        }

        if (ollamaProcesses.Count == 0)
        {
            throw new InvalidOperationException(
                $"На порту {OllamaPort} не найден процесс ollama.exe. Сервер мог уже остановиться.");
        }

        foreach (var process in ollamaProcesses)
        {
            using (process)
            {
                process.Kill(entireProcessTree: true);
                if (!process.WaitForExit(5000))
                {
                    throw new TimeoutException(
                        $"Процесс ollama.exe (PID {process.Id}) не завершился за 5 секунд.");
                }
            }
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static string? FindStartupScript()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "ollama-vulkan",
                "start-ollama.cmd");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var installedScript = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Ollama Vulkan",
            "start-ollama.cmd");

        return File.Exists(installedScript) ? installedScript : null;
    }
}
