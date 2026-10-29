using System.Text.Json;

namespace WalkBreakSoftReminder;

internal sealed record AppSettings(int SeatedMinutes = 30, int WalkMinutes = 5)
{
    public const int MaximumMinutes = 1440;

    public bool IsValid => SeatedMinutes is >= 1 and <= MaximumMinutes
        && WalkMinutes is >= 1 and <= MaximumMinutes;

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WalkBreakSoftReminder", "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath))
            return new();

        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
        if (settings is null || !settings.IsValid)
            throw new InvalidDataException("Durations must be between 1 and 1440 minutes.");

        return settings;
    }

    public void Save()
    {
        if (!IsValid)
            throw new InvalidDataException("Durations must be between 1 and 1440 minutes.");

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, FilePath, overwrite: true);
    }
}
