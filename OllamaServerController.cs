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

    public void StartServer()
    {
        var startupScript = FindStartupScript();
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/d /c \"\"{startupScript}\" /hidden\"",
            WorkingDirectory = Path.GetDirectoryName(startupScript)!,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        if (Process.Start(startInfo) is null)
        {
            throw new InvalidOperationException("Не удалось запустить процесс cmd.exe.");
        }
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

    private static string FindStartupScript()
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

        throw new FileNotFoundException(
            "Не найден ollama-vulkan\\start-ollama.cmd. " +
            "Поместите приложение в подпапку рядом с каталогом ollama-vulkan.");
    }
}
