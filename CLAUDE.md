# CoffeeUpdateClient

## What This Is

A WPF Windows desktop app that auto-discovers a World of Warcraft installation via the registry, then downloads and installs (or updates) specific WoW addons from a remote manifest hosted on DigitalOcean Spaces. The app is distributed as a single self-contained `.exe` and self-updates via AutoUpdater.NET.

## Build & Run

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Publish single-file exe (release)
dotnet publish -c Release

# Format check (enforced by CI)
dotnet format --verify-no-changes
```

Output goes to `build/` (set via `Directory.Build.props`).

## Project Structure

```
src/CoffeeUpdateClient/
  App.xaml.cs          # DI setup, Serilog init, AutoUpdater config, startup
  MainWindow.xaml.cs   # UI + inner State class (MVVM via PropertyChanged.Fody)
  Models/              # Config, AddOnMetadata, AddOnBundle, AddOnManifest, AddOnInstallState
  Services/            # IEnv/WindowsEnv, IConfigService, IWoWLocator, IAddOnDownloader + impls
  Utils/               # AddOnUpdateManager, AddOnBundleInstaller, LocalAddOnMetadataLoader,
                       # AddOnPathResolver, TOCParser, InstallLogCollector, AppDataFolder
tests/CoffeeUpdateClient.Tests/
  Mocks/               # MockEnv, MockWowLocator, MockConfigService, MockAddOnDownloader
  *Test.cs             # NUnit 4 test files
```

## Key Design Patterns

- **DI container**: `Microsoft.Extensions.DependencyInjection`, all singletons, built in `App.xaml.cs`
- **File system abstraction**: `TestableIO.System.IO.Abstractions` used throughout for testability; `IEnv` provides `IFileSystem`
- **MVVM**: `MainWindow` has an inner `State` class bound to `DataContext`; property notifications woven by `PropertyChanged.Fody`
- **Config singleton**: `Config` uses a static singleton (`Config.Instance`) initialized from DI at startup. Tests use `ConfigTestBase` to init/clear it around each test
- **Logging**: Serilog to console + rolling file at `%AppData%\Roaming\CoffeeUpdateClient\client.log`

## Addon Path Expectations

The app expects the WoW addons folder to be at `..._retail_\Interface\AddOns`. `AddOnPathResolver.NormalizeAddOnsDirectory` auto-completes partial paths (e.g., passing just the WoW install directory appends `_retail_\Interface\AddOns`). Path segment matching is **case-sensitive by design**.

## Addon TOC File Lookup Order

For each addon, `LocalAddOnMetadataLoader` tries these filenames in order:
1. `{Name}.toc`
2. `{Name}_Mainline.toc`
3. `{Name}_Standard.toc`

The first file found with a `## Version:` line wins.

## Release Pipeline

1. Push a semver tag (`*.*.*`) → `release.yml` builds, publishes, creates a GitHub release ZIP
2. After release completes → `upload-release-to-s3.js` uploads the ZIP and generates `client.xml` for AutoUpdater.NET
3. On next app launch, AutoUpdater.NET checks `client.xml`, prompts for update (mandatory, no skip/remind)

AutoUpdater is disabled in `#if DEBUG` builds.