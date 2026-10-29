namespace WalkBreakSoftReminder;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var instance = new Mutex(true, @"Local\WalkBreakSoftReminder", out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("Walk Break Soft Reminder is already running. Look for its icon in the notification area.", "Walk Break Soft Reminder");
            return;
        }

        AppSettings settings;
        try
        {
            settings = AppSettings.Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            MessageBox.Show($"Could not load settings. Using the defaults.\n\n{ex.Message}", "Walk Break Soft Reminder");
            settings = new();
        }

        using var context = new TrayApplicationContext(settings);
        Application.Run(context);
    }
}
