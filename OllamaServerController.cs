using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;

namespace OllamaTray;

internal sealed class OllamaServerController : IDisposable
{
    private static readonly IPAddress AnyAddress = IPAddress.Any;
    private const int OllamaPort = 11434;
    private readonly OllamaSettingsStore _settingsStore = new();
    private OllamaSettings _settings;
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    public OllamaServerController()
    {
        _settings = _settingsStore.Load();
    }

    public OllamaSettings Settings => _settings.Copy();

    public string ApiBaseUrl => GetApiBaseUri(_settings.Host).ToString().TrimEnd('/');

    public event Action<Exception>? LogWriteFailed;

    public void SaveSettings(OllamaSettings settings)
    {
        _settingsStore.Save(settings);
        _settings = settings.Copy();
    }

    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                new Uri(GetApiBaseUri(_settings.Host), "api/version"));
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
        var executable = FindOllamaExecutable();
        var logDirectory = Path.Combine(Path.GetDirectoryName(executable)!, "logs");
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, "ollama.log");
        var logWriter = new StreamWriter(
            new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
        var startInfo = CreateStartInfo(executable);
        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException(LocalizedText.Get("ErrorStartProcess"));
        }
        catch (Win32Exception exception)
        {
            logWriter.Dispose();
            throw new InvalidOperationException(
                LocalizedText.Format("ErrorStartExecutable", executable),
                exception);
        }
        catch
        {
            logWriter.Dispose();
            throw;
        }

        var startupCheckCompleted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _ = LogProcessOutputAsync(
            process,
            logWriter,
            SynchronizationContext.Current,
            startupCheckCompleted.Task);
        try
        {
            await WaitForServerAsync(process);
        }
        finally
        {
            startupCheckCompleted.SetResult();
        }
    }

    private async Task LogProcessOutputAsync(
        Process process,
        StreamWriter logWriter,
        SynchronizationContext? synchronizationContext,
        Task startupCheckCompleted)
    {
        Exception? logError = null;
        async Task CopyLinesAsync(StreamReader reader, string streamName)
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                lock (logWriter)
                {
                    if (logError is null)
                    {
                        try
                        {
                            logWriter.WriteLine(
                                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{streamName}] {line}");
                        }
                        catch (IOException exception)
                        {
                            logError = exception;
                        }
                        catch (ObjectDisposedException exception)
                        {
                            logError = exception;
                        }
                    }
                }
            }
        }

        try
        {
            await Task.WhenAll(
                CopyLinesAsync(process.StandardOutput, "stdout"),
                CopyLinesAsync(process.StandardError, "stderr"));
        }
        catch (IOException exception)
        {
            logError ??= exception;
        }
        finally
        {
            try
            {
                logWriter.Dispose();
            }
            catch (IOException exception)
            {
                logError ??= exception;
            }

            if (logError is not null)
            {
                ReportLogWriteFailure(logError, synchronizationContext);
            }

            await startupCheckCompleted;
            process.Dispose();
        }
    }

    private void ReportLogWriteFailure(
        Exception exception,
        SynchronizationContext? synchronizationContext)
    {
        if (synchronizationContext is null)
        {
            LogWriteFailed?.Invoke(exception);
        }
        else
        {
            synchronizationContext.Post(
                _ => LogWriteFailed?.Invoke(exception),
                null);
        }
    }

    private async Task WaitForServerAsync(Process process)
    {
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
                    LocalizedText.Format("ErrorServerExited", process.ExitCode));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            LocalizedText.Format("ErrorServerStartTimeout", timeout.TotalSeconds));
    }

    private ProcessStartInfo CreateStartInfo(string executable)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "serve",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.WorkingDirectory = Path.GetDirectoryName(executable)!;

        startInfo.Environment["OLLAMA_MODELS"] =
            Environment.ExpandEnvironmentVariables(_settings.ModelsDirectory);
        startInfo.Environment["OLLAMA_HOST"] = _settings.Host;
        startInfo.Environment["OLLAMA_CONTEXT_LENGTH"] =
            _settings.ContextLength.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["OLLAMA_KEEP_ALIVE"] = _settings.KeepAlive;

        return startInfo;
    }

    public static bool TryCreateApiBaseUri(string host, out Uri? apiBaseUri)
    {
        apiBaseUri = null;
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var hostValue = host.Trim();
        var portSeparator = hostValue.StartsWith("[", StringComparison.Ordinal)
            ? hostValue.IndexOf("]:", StringComparison.Ordinal)
            : hostValue.LastIndexOf(':');
        var hasExplicitPort = portSeparator >= 0;
        var portTextStart = portSeparator +
            (hostValue.StartsWith("[", StringComparison.Ordinal) ? 2 : 1);
        if (hasExplicitPort &&
            (!int.TryParse(
                hostValue.AsSpan(portTextStart),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var explicitPort) ||
             explicitPort is < 1 or > 65535))
        {
            return false;
        }

        if (!Uri.TryCreate($"http://{hostValue}", UriKind.Absolute, out var configuredUri) ||
            configuredUri.UserInfo.Length != 0 ||
            configuredUri.AbsolutePath != "/" ||
            configuredUri.Query.Length != 0 ||
            configuredUri.Fragment.Length != 0)
        {
            return false;
        }

        var hostname = configuredUri.Host;
        var parsedAddressText = hostname.Trim('[', ']');
        if (IPAddress.TryParse(parsedAddressText, out var configuredAddress) &&
            configuredAddress.Equals(IPAddress.Any))
        {
            hostname = IPAddress.Loopback.ToString();
        }
        else if (IPAddress.TryParse(parsedAddressText, out configuredAddress) &&
                 configuredAddress.Equals(IPAddress.IPv6Any))
        {
            hostname = IPAddress.IPv6Loopback.ToString();
        }

        var port = hasExplicitPort ? configuredUri.Port : OllamaPort;
        if (port is < 1 or > 65535)
        {
            return false;
        }

        try
        {
            apiBaseUri = new UriBuilder(Uri.UriSchemeHttp, hostname, port).Uri;
            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    private static Uri GetApiBaseUri(string host)
    {
        if (TryCreateApiBaseUri(host, out var apiBaseUri))
        {
            return apiBaseUri!;
        }

        throw new InvalidDataException(LocalizedText.Get("ErrorHostInvalid"));
    }

    public void OpenLogFile()
    {
        var executable = FindOllamaExecutable();
        var logPath = Path.Combine(
            Path.GetDirectoryName(executable)!,
            "logs",
            "ollama.log");

        if (!File.Exists(logPath))
        {
            throw new FileNotFoundException(
                LocalizedText.Format("ErrorLogMissing", logPath),
                logPath);
        }

        using var viewer = Process.Start(new ProcessStartInfo
        {
            FileName = logPath,
            UseShellExecute = true
        }) ?? throw new InvalidOperationException(LocalizedText.Get("ErrorOpenLog"));
    }

    public void StopServer()
    {
        var port = GetApiBaseUri(_settings.Host).Port;
        var processIds = WindowsTcpTable.GetListeningProcessIds(
            port,
            AnyAddress,
            IPAddress.IPv6Any);
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
                LocalizedText.Format("ErrorNoServerProcess", port));
        }

        foreach (var process in ollamaProcesses)
        {
            using (process)
            {
                process.Kill(entireProcessTree: true);
                if (!process.WaitForExit(5000))
                {
                    throw new TimeoutException(
                        LocalizedText.Format("ErrorProcessStopTimeout", process.Id));
                }
            }
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static string FindOllamaExecutable()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "ollama-vulkan",
                "ollama.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var installedExecutable = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Ollama Vulkan",
            "ollama.exe");

        if (File.Exists(installedExecutable))
        {
            return installedExecutable;
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (path is not null)
        {
            foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var normalizedDirectory = directory.Trim().Trim('"');
                var candidate = Path.Combine(normalizedDirectory, "ollama.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new FileNotFoundException(LocalizedText.Get("ErrorExecutableNotFound"));
    }
}
