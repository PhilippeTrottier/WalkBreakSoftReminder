# Walk Break Soft Reminder

A quiet Windows notification-area reminder to stand up and walk. Built with C#,
.NET 10 LTS, and Windows Forms. No third-party packages.

Download `WalkBreakSoftReminder.exe` from the [latest release](https://github.com/PhilippeTrottier/WalkBreakSoftReminder/releases/latest).
It is a self-contained Windows x64 executable; no separate .NET installation is needed.

## Run

On Windows with the .NET 10 SDK:

```powershell
dotnet run --project WalkBreakSoftReminder
```

The app starts seated with no main window. Look in the notification area (including
the hidden-icons overflow). Left or right click its circular icon to open the menu.
Only one instance runs per Windows session. Exit removes the tray icon.

| State | Icon | Actions |
| --- | --- | --- |
| Seated (initially 30 minutes) | Muted gray | Walk, Reset |
| Time to walk | Red → white → red, every two seconds | Walk, Reset |
| Walking (initially 5 minutes), first 70% | Yellow → white → yellow, every two seconds | Reset |
| Walking, final 30% | Solid blue | Reset |

The menu always includes Configure, Exit, and a countdown to the next automatic
state transition. A warning waits for Walk or Reset, so it shows a prompt instead
of a countdown. Reset always starts a full seated interval, including when returning
early from a walk. A completed walk automatically starts a full seated interval.
There are no sounds, popups, or automatic idle detection.

Configure accepts whole-minute durations from 1 to 1440. Save preserves elapsed
time in the current state; shortening a duration past its elapsed time transitions
immediately. A warning stays active until Walk or Reset. Cancel discards edits.
Durations are saved to `%LOCALAPPDATA%\WalkBreakSoftReminder\settings.json`. Each app launch
starts a fresh seated timer. Invalid or unreadable settings produce a message and
fall back to 30/5 minutes.

Start on user logon defaults to off. Enabling it registers the current executable
in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` as `WalkBreakSoftReminder`. No admin
rights are needed. Publish to a permanent folder before enabling this; if you move
the executable, save the toggle again from its new location. Windows Startup Apps
can separately disable startup entries.

Timers use UTC deadlines, so sleeping does not pause the interval. On waking, an
expired seated timer warns; an expired walk starts one fresh seated interval.
Changing the system clock also affects deadlines.

## Build and check

```powershell
dotnet build WalkBreakSoftReminder.slnx --configuration Release
dotnet run --project WalkBreakSoftReminder.Checks --configuration Release
```

The checks are a small console program with no test packages. A failed check throws
and returns a nonzero exit code. They cover state transitions, timer boundaries,
configuration changes, validation, native icon creation/disposal, and form Save/Cancel.
The form checks briefly show a window. To preview
the configuration form without writing settings or changing logon startup:

```powershell
dotnet run --project WalkBreakSoftReminder.Checks -- --preview
```

To publish the same single, self-contained Windows x64 executable as CI:

```powershell
dotnet publish WalkBreakSoftReminder --configuration Release --runtime win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None `
  --output publish/single-file
.\publish\single-file\WalkBreakSoftReminder.exe
```

The runtime is bundled into the EXE, making it larger. Native components extract
internally when it runs. Settings still go into Local AppData.

## Automatic releases

Every push to `main` runs `.github/workflows/release.yml` on a standard GitHub-hosted
Windows runner. It selects .NET 10, builds, runs the checks, publishes, and verifies
that the output contains exactly one EXE. Only successful runs create a release,
tagged `build-1`, `build-2`, and so on, pointing to the pushed commit. The EXE is
attached directly to the release, with generated release notes.

The workflow uses GitHub's own checkout/setup actions and the runner's GitHub CLI.
Its built-in token has `contents: write` permission to create tags and releases;
no additional credentials are required. Standard hosted runners are free for
this public repository.

Exit the app before rebuilding or replacing its executable.

## Code map

- `ReminderTimer.cs`: the three states and time calculations; no UI or I/O.
- `TrayApplicationContext.cs`: menu, 100 ms UI timer, and application lifetime.
- `TrayIcons.cs`: reusable animation frames and native icon handle cleanup.
- `ConfigurationForm.cs`: a small form built with standard controls.
- `AppSettings.cs`: duration validation and JSON persistence.
- `StartupRegistration.cs`: the current user's Windows startup entry.
- `Program.cs`: initialization, single-instance guard, and settings fallback.

The one native API call, `DestroyIcon`, releases handles created by
`Bitmap.GetHicon`. Everything else uses the .NET and Windows Forms libraries.
