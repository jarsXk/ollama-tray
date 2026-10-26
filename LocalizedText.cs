using System.Globalization;
using System.Resources;

namespace OllamaTray;

internal static class LocalizedText
{
    private static readonly ResourceManager ResourceManager =
        new("OllamaTray.Strings", typeof(LocalizedText).Assembly);

    public static string Get(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(
                $"Missing localized string resource: {name}");
    }

    public static string Format(string name, params object[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(name), arguments);
    }
}
