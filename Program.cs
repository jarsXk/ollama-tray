namespace OllamaTray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        LocalizedText.SetLanguage(ApplicationLanguage.System);
        ApplicationConfiguration.Initialize();
        try
        {
            var settings = new OllamaSettingsStore().Load();
            LocalizedText.SetLanguage(settings.Language);
            Application.Run(new OllamaTrayApplicationContext(settings));
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
