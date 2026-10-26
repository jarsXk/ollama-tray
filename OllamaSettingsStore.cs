using System.Text.Json;

namespace OllamaTray;

internal sealed class OllamaSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OllamaTray",
        "settings.json");

    public OllamaSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new OllamaSettings();
        }

        var json = File.ReadAllText(_settingsPath);
        OllamaSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<OllamaSettings>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                LocalizedText.Format("ErrorSettingsInvalid", _settingsPath),
                exception);
        }

        if (settings is null)
        {
            throw new InvalidDataException(
                LocalizedText.Format("ErrorSettingsInvalid", _settingsPath));
        }

        Validate(settings);
        return settings;
    }

    public void Save(OllamaSettings settings)
    {
        Validate(settings);
        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException(
                LocalizedText.Get("ErrorSettingsDirectory"));
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static void Validate(OllamaSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ModelsDirectory) ||
            !OllamaServerController.TryCreateApiBaseUri(settings.Host, out _) ||
            settings.ContextLength <= 0 ||
            string.IsNullOrWhiteSpace(settings.KeepAlive))
        {
            throw new InvalidDataException(
                LocalizedText.Get("ErrorSettingsValuesInvalid"));
        }
    }
}
