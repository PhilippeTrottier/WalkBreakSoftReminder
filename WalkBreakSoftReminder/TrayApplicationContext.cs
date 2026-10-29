namespace WalkBreakSoftReminder;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private AppSettings settings;
    private readonly ReminderTimer reminder;
    private readonly TrayIcons icons = new();
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem countdown = new() { Enabled = false };
    private readonly ToolStripMenuItem walk = new("Walk");
    private readonly System.Windows.Forms.Timer ticker = new() { Interval = 100 };
    private readonly NotifyIcon tray;
    private ConfigurationForm? configuration;

    public TrayApplicationContext(AppSettings settings)
    {
        this.settings = settings;
        reminder = new(settings, DateTimeOffset.UtcNow);

        menu.Items.Add("Configure", null, (_, _) => Configure());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(countdown);
        menu.Items.Add(walk);
        menu.Items.Add("Reset", null, (_, _) =>
        {
            reminder.Reset(DateTimeOffset.UtcNow);
            RefreshTray();
        });
        walk.Click += (_, _) =>
        {
            reminder.Walk(DateTimeOffset.UtcNow);
            RefreshTray();
        };
        menu.Opening += (_, _) => RefreshTray();

        tray = new NotifyIcon { ContextMenuStrip = menu, Icon = icons.Seated, Text = "Walk Break Soft Reminder", Visible = true };
        tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                menu.Show(Cursor.Position);
        };
        ticker.Tick += (_, _) => RefreshTray();
        RefreshTray();
        ticker.Start();
    }

    private void RefreshTray()
    {
        var now = DateTimeOffset.UtcNow;
        reminder.Update(now);
        var remaining = TimeSpan.FromSeconds(Math.Ceiling(reminder.Remaining(now).TotalSeconds));
        var time = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        countdown.Text = reminder.State switch
        {
            ReminderState.Seated => $"Seated · walk in {time}",
            ReminderState.Warning => "Time to stand up and walk · waiting for Walk",
            _ => $"Walking · seated in {time}"
        };
        walk.Visible = reminder.State != ReminderState.Walking;
        tray.Text = reminder.State == ReminderState.Warning ? "Walk Break Soft Reminder: time to stand up and walk" : $"Walk Break Soft Reminder: {countdown.Text}";

        // A complete pulse takes two seconds; no images are allocated on each tick.
        var frame = (int)(Environment.TickCount64 / ticker.Interval % icons.Warning.Length);
        tray.Icon = reminder.State switch
        {
            ReminderState.Seated => icons.Seated,
            ReminderState.Warning => icons.Warning[frame],
            _ => reminder.WalkIsNearlyDone(now) ? icons.NearlyDone : icons.Walking[frame]
        };
    }

    private void Configure()
    {
        if (configuration is not null)
        {
            configuration.Activate();
            return;
        }

        bool startsOnLogon;
        try
        {
            startsOnLogon = StartupRegistration.IsEnabled();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            MessageBox.Show($"Could not read the logon setting.\n\n{ex.Message}", "Walk Break Soft Reminder");
            return;
        }

        configuration = new ConfigurationForm(settings, startsOnLogon, (newSettings, startup) =>
        {
            newSettings.Save();
            settings = newSettings;
            reminder.Configure(settings, DateTimeOffset.UtcNow);
            RefreshTray();
            StartupRegistration.SetEnabled(startup);
        });
        configuration.FormClosed += (_, _) => configuration = null;
        configuration.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ticker.Dispose();
            configuration?.Dispose();
            tray.Visible = false;
            tray.Dispose();
            menu.Dispose();
            icons.Dispose();
        }
        base.Dispose(disposing);
    }
}
