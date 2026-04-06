# CoffeeUpdater

## What This Is

A WPF Windows desktop app that auto-discovers a World of Warcraft installation via the registry, then downloads and installs (or updates) specific WoW addons from a remote manifest hosted on DigitalOcean Spaces. The app runs as a system tray background application, polling for addon updates every 30 seconds. It is distributed via Velopack (installer + auto-updater) and auto-starts on Windows login.

## Build & Run

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run tests with coverage (requires: dotnet tool install -g dotnet-coverage)
dotnet-coverage collect -s coverage.settings.xml -o build/TestResults/coverage.cobertura.xml -f cobertura "dotnet test"

# Generate HTML coverage report (requires: dotnet tool install -g dotnet-reportgenerator-globaltool)
reportgenerator -reports:build/TestResults/coverage.cobertura.xml -targetdir:build/CoverageReport -reporttypes:Html

# Publish single-file exe (release)
dotnet publish -c Release

# Format check (enforced by CI)
dotnet format --verify-no-changes
```

Output goes to `build/` (set via `Directory.Build.props`).

## Project Structure

```
src/CoffeeUpdater/
  App.xaml.cs          # DI setup, Serilog init, Velopack hooks, tray icon, startup
  App.xaml             # TaskbarIcon resource with context menu
  MainWindow.xaml.cs   # Status window UI + inner State class (MVVM via PropertyChanged.Fody)
  Models/              # Config, AddOnMetadata, AddOnBundle, AddOnManifest, AddOnInstallState, ManifestResult
  Services/            # IEnv/WindowsEnv, IConfigService, IWoWLocator, IAddOnDownloader + impls,
                       # AddOnSyncService (BackgroundService for periodic polling)
  Utils/               # AddOnUpdateManager, AddOnBundleInstaller, LocalAddOnMetadataLoader,
                       # AddOnPathResolver, TOCParser, InstallLogCollector, AppDataFolder,
                       # SingleInstanceGuard, VelopackHooks, RelayCommand
tests/CoffeeUpdater.Tests/
  Mocks/               # MockEnv, MockWowLocator, MockConfigService, MockAddOnDownloader
  *Test.cs             # NUnit 4 test files
```

## Key Design Patterns

- **System tray app**: Runs as a background tray icon (Hardcodet.NotifyIcon.Wpf). MainWindow is a show/hide status window, not the primary lifecycle owner. `ShutdownMode.OnExplicitShutdown`.
- **Background sync**: `AddOnSyncService` (a `BackgroundService`) polls the manifest every 30s using `PeriodicTimer`. Uses ETag-based conditional HTTP requests to skip work when nothing changed.
- **Single instance**: `SingleInstanceGuard` uses a named Mutex + named pipe IPC. Second launches signal the running instance to show its window, then exit.
- **DI container**: `Microsoft.Extensions.DependencyInjection` + `Microsoft.Extensions.Hosting`, all singletons, built in `App.xaml.cs`
- **File system abstraction**: `TestableIO.System.IO.Abstractions` used throughout for testability; `IEnv` provides `IFileSystem`
- **MVVM**: `MainWindow` has an inner `State` class bound to `DataContext`; property notifications woven by `PropertyChanged.Fody`
- **Logging**: Serilog to console + rolling file at `%AppData%\Roaming\CoffeeUpdater\client.log`

## Addon Path Expectations

The app expects the WoW addons folder to be at `..._retail_\Interface\AddOns`. `AddOnPathResolver.NormalizeAddOnsDirectory` auto-completes partial paths (e.g., passing just the WoW install directory appends `_retail_\Interface\AddOns`). Path segment matching is **case-sensitive by design**.

## Addon TOC File Lookup Order

For each addon, `LocalAddOnMetadataLoader` tries these filenames in order:
1. `{Name}.toc`
2. `{Name}_Mainline.toc`
3. `{Name}_Standard.toc`

The first file found with a `## Version:` line wins.

## Auto-Start & Installation

- Velopack installs to `%LocalAppData%\CoffeeUpdater` (per-user, no admin required)
- On install, `VelopackHooks.OnInstall()` sets a Registry Run key (`HKCU\...\Run`) so the app starts automatically on login
- On uninstall, the Run key is removed
- Auto-start is always on; there is no user toggle (all users are known and expect this behavior)

## Release Pipeline

1. Push a semver tag (`*.*.*`) → `release.yml` builds, publishes, runs `vpk pack` to produce Velopack artifacts, creates a GitHub release with all artifacts
2. After release completes → `upload-release-to-s3.js` uploads all Velopack artifacts to the `releases/` prefix on DigitalOcean Spaces
3. Running app checks for self-updates via Velopack's `UpdateManager` pointing at the `releases/` URL

Velopack update checks are disabled in `#if DEBUG` builds.
