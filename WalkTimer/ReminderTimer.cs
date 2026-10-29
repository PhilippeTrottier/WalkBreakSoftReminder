namespace WalkTimer;

internal enum ReminderState { Seated, Warning, Walking }

// Time is supplied by the caller so the transitions can be checked without waiting.
internal sealed class ReminderTimer(AppSettings settings, DateTimeOffset now)
{
    private AppSettings settings = settings;
    private DateTimeOffset startedAt = now;

    public ReminderState State { get; private set; } = ReminderState.Seated;

    public TimeSpan Remaining(DateTimeOffset now)
    {
        if (State == ReminderState.Warning)
            return TimeSpan.Zero;

        var minutes = State == ReminderState.Walking ? settings.WalkMinutes : settings.SeatedMinutes;
        return TimeSpan.FromSeconds(Math.Max(0, (TimeSpan.FromMinutes(minutes) - (now - startedAt)).TotalSeconds));
    }

    public bool WalkIsNearlyDone(DateTimeOffset now) =>
        State == ReminderState.Walking && now - startedAt >= TimeSpan.FromMinutes(settings.WalkMinutes * 0.7);

    public void Update(DateTimeOffset now)
    {
        if (Remaining(now) > TimeSpan.Zero)
            return;

        if (State == ReminderState.Seated)
            State = ReminderState.Warning;
        else if (State == ReminderState.Walking)
            Reset(now);
    }

    public void Walk(DateTimeOffset now)
    {
        State = ReminderState.Walking;
        startedAt = now;
    }

    public void Reset(DateTimeOffset now)
    {
        State = ReminderState.Seated;
        startedAt = now;
    }

    public void Configure(AppSettings newSettings, DateTimeOffset now)
    {
        settings = newSettings;
        Update(now);
    }
}
