namespace OllamaTray;

internal enum ApplicationLanguage
{
    System,
    English,
    Russian
}

internal sealed class OllamaSettings
{
    public string ModelsDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".ollama",
        "models");

    public string Host { get; set; } = "127.0.0.1:11434";

    public int ContextLength { get; set; } = 8192;

    public string KeepAlive { get; set; } = "5m";

    public bool StartServerOnTrayLaunch { get; set; }

    public bool StopServerOnTrayExit { get; set; }

    public ApplicationLanguage Language { get; set; } = ApplicationLanguage.System;

    public OllamaSettings Copy()
    {
        return new OllamaSettings
        {
            ModelsDirectory = ModelsDirectory,
            Host = Host,
            ContextLength = ContextLength,
            KeepAlive = KeepAlive,
            StartServerOnTrayLaunch = StartServerOnTrayLaunch,
            StopServerOnTrayExit = StopServerOnTrayExit,
            Language = Language
        };
    }
}
