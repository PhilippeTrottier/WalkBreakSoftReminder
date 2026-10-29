using WalkTimer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--preview"))
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ConfigurationForm(new AppSettings(), false, (_, _) => { }));
            return;
        }

        // A package-free executable: a failed check throws and produces a nonzero exit code.
        var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timer = new ReminderTimer(new AppSettings(), start);
        var checks = 0;

        void Check(bool condition, string description)
        {
            if (!condition)
                throw new InvalidOperationException(description);
            Console.WriteLine($"PASS: {description}");
            checks++;
        }

        Check(timer.State == ReminderState.Seated && timer.Remaining(start) == TimeSpan.FromMinutes(30), "Starts seated for 30 minutes");
        timer.Update(start.AddMinutes(30).AddTicks(-1));
        Check(timer.State == ReminderState.Seated, "Stays seated before the deadline");
        timer.Update(start.AddMinutes(30));
        Check(timer.State == ReminderState.Warning && timer.Remaining(start.AddMinutes(31)) == TimeSpan.Zero, "Warns at the exact seated deadline");
        timer.Update(start.AddDays(1));
        Check(timer.State == ReminderState.Warning, "Warning waits for user action");
        timer.Reset(start);
        Check(timer.State == ReminderState.Seated && timer.Remaining(start) == TimeSpan.FromMinutes(30), "Reset clears warning and restarts seated time");
        timer.Reset(start.AddMinutes(10));
        Check(timer.Remaining(start.AddMinutes(10)) == TimeSpan.FromMinutes(30), "Reset restarts seated time even when already seated");

        timer.Walk(start);
        Check(timer.State == ReminderState.Walking && timer.Remaining(start) == TimeSpan.FromMinutes(5), "Walk can start early from seated");
        Check(!timer.WalkIsNearlyDone(start.AddSeconds(210).AddTicks(-1)), "Walking pulses before 70 percent");
        Check(timer.WalkIsNearlyDone(start.AddSeconds(210)), "Walking turns blue at exactly 70 percent");
        timer.Update(start.AddMinutes(5).AddTicks(-1));
        Check(timer.State == ReminderState.Walking, "Walking continues until its deadline");
        timer.Update(start.AddMinutes(5));
        Check(timer.State == ReminderState.Seated && timer.Remaining(start.AddMinutes(5)) == TimeSpan.FromMinutes(30), "Completed walk starts a fresh seated interval");
        timer.Walk(start);
        timer.Reset(start.AddMinutes(1));
        Check(timer.State == ReminderState.Seated && timer.Remaining(start.AddMinutes(1)) == TimeSpan.FromMinutes(30), "Returning early restarts seated time");

        timer.Reset(start);
        timer.Update(start.AddMinutes(30));
        timer.Walk(start.AddMinutes(31));
        Check(timer.State == ReminderState.Walking, "Walk starts from warning");
        timer.Update(start.AddDays(1));
        Check(timer.State == ReminderState.Seated && timer.Remaining(start.AddDays(1)) == TimeSpan.FromMinutes(30), "Late wakeup after walking starts one fresh seated interval");

        timer.Reset(start);
        timer.Configure(new AppSettings(20, 10), start.AddMinutes(10));
        Check(timer.Remaining(start.AddMinutes(10)) == TimeSpan.FromMinutes(10), "Configure preserves elapsed seated time");
        timer.Configure(new AppSettings(5, 10), start.AddMinutes(10));
        Check(timer.State == ReminderState.Warning, "Shortening an elapsed seated duration warns immediately");
        timer.Configure(new AppSettings(60, 10), start.AddMinutes(10));
        Check(timer.State == ReminderState.Warning, "Configuration does not dismiss a warning");
        timer.Walk(start);
        timer.Configure(new AppSettings(60, 20), start.AddMinutes(5));
        Check(timer.Remaining(start.AddMinutes(5)) == TimeSpan.FromMinutes(15), "Configure preserves elapsed walking time");
        timer.Configure(new AppSettings(60, 1), start.AddMinutes(5));
        Check(timer.State == ReminderState.Seated && timer.Remaining(start.AddMinutes(5)) == TimeSpan.FromMinutes(60), "Shortening an elapsed walk completes it immediately");
        Check(new AppSettings().IsValid && new AppSettings(1, 1440).IsValid, "Default and boundary settings are valid");
        Check(!new AppSettings(0, 5).IsValid && !new AppSettings(30, 1441).IsValid, "Invalid durations are rejected");

        using (var icons = new TrayIcons())
        {
            Check(icons.Warning.Length == 20 && icons.Walking.Length == 20, "Native pulse icons can be created and disposed");
        }

        var saved = false;
        using (var form = new ConfigurationForm(new AppSettings(), false, (_, _) => saved = true))
        {
            form.Show();
            form.CancelButton!.PerformClick();
            Check(form.IsDisposed && !saved, "Cancel closes the modeless configuration form without saving");
        }

        using (var form = new ConfigurationForm(new AppSettings(), true, (settings, startup) =>
        {
            saved = settings == new AppSettings() && startup;
        }))
        {
            form.Show();
            form.AcceptButton!.PerformClick();
            Check(form.IsDisposed && saved, "Save passes durations and startup choice, then closes the form");
        }

        Console.WriteLine($"All {checks} checks passed.");
    }
}
