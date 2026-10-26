using System.Globalization;

namespace OllamaTray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var windowsCulture = CultureInfo.CurrentUICulture;
        var applicationCulture = windowsCulture.TwoLetterISOLanguageName == "ru"
            ? CultureInfo.GetCultureInfo("ru")
            : CultureInfo.GetCultureInfo("en");
        CultureInfo.CurrentCulture = applicationCulture;
        CultureInfo.CurrentUICulture = applicationCulture;
        CultureInfo.DefaultThreadCurrentCulture = applicationCulture;
        CultureInfo.DefaultThreadCurrentUICulture = applicationCulture;

        ApplicationConfiguration.Initialize();
        try
        {
            Application.Run(new OllamaTrayApplicationContext());
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                LocalizedText.Format("ErrorApplicationStartup", exception.Message),
                LocalizedText.Get("TrayTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
