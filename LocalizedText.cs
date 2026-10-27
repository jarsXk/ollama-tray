using System.Globalization;
using System.Resources;

namespace OllamaTray;

internal static class LocalizedText
{
    private static readonly CultureInfo WindowsCulture = CultureInfo.CurrentUICulture;
    private static readonly ResourceManager ResourceManager =
        new("OllamaTray.Strings", typeof(LocalizedText).Assembly);

    public static void SetLanguage(ApplicationLanguage language)
    {
        var applicationCulture = language switch
        {
            ApplicationLanguage.English => CultureInfo.GetCultureInfo("en"),
            ApplicationLanguage.Russian => CultureInfo.GetCultureInfo("ru"),
            _ => WindowsCulture.TwoLetterISOLanguageName == "ru"
                ? CultureInfo.GetCultureInfo("ru")
                : CultureInfo.GetCultureInfo("en")
        };

        CultureInfo.CurrentCulture = applicationCulture;
        CultureInfo.CurrentUICulture = applicationCulture;
        CultureInfo.DefaultThreadCurrentCulture = applicationCulture;
        CultureInfo.DefaultThreadCurrentUICulture = applicationCulture;
    }

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
