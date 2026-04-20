using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using Serilog;

namespace CoffeeUpdater.Utils;

public class AddOnBundleInstaller
{
    private readonly IFileSystem _fileSystem;
    private readonly Config _config;

    public AddOnBundleInstaller(IEnv env, Config config)
    {
        _fileSystem = env.FileSystem;
        _config = config;
    }

    public List<string> InstallAddOn(AddOnBundle bundle)
    {
        var addOnsPath = _config.AddOnsPath;
        var interfacePath = _fileSystem.Path.GetDirectoryName(addOnsPath)!;
        var stagingPath = _fileSystem.Path.Combine(interfacePath, "CoffeeUpdaterStaging", $"{bundle.Metadata.Name}-{Guid.NewGuid()}");
        _fileSystem.Directory.CreateDirectory(stagingPath);
        Log.Information("Extracting add-on bundle for '{AddOnName}' to staging path: {StagingPath}", bundle.Metadata.Name, stagingPath);

        try
        {
            using (var archive = new ZipArchive(bundle.Data, ZipArchiveMode.Read))
            {
                var rootFolders = archive.Entries
                    .Select(e => e.FullName.Split(['/', '\\'])[0])
                    .Distinct()
                    .ToList();

                if (!rootFolders.Any(f => string.Equals(f, bundle.Metadata.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"AddOn bundle for '{bundle.Metadata.Name}' must contain a root folder named '{bundle.Metadata.Name}' for version detection. Found: {string.Join(", ", rootFolders)}");
                }

                foreach (var entry in archive.Entries)
                {
                    var fullPath = _fileSystem.Path.Combine(stagingPath, entry.FullName).Replace("/", _fileSystem.Path.DirectorySeparatorChar.ToString());
                    Log.Verbose("Processing entry: {EntryName} -> {FullPath}", entry.Name, fullPath);

                    if (entry.Name == "")
                    {
                        if (!_fileSystem.Directory.Exists(fullPath))
                        {
                            _fileSystem.Directory.CreateDirectory(fullPath);
                        }
                    }
                    else
                    {
                        var directoryPath = _fileSystem.Path.GetDirectoryName(fullPath);
                        if (directoryPath != null && !_fileSystem.Directory.Exists(directoryPath))
                        {
                            _fileSystem.Directory.CreateDirectory(directoryPath);
                        }

                        using (var entryStream = entry.Open())
                        using (var fileStream = _fileSystem.File.Create(fullPath))
                        {
                            entryStream.CopyTo(fileStream);
                        }
                    }
                }

                foreach (var folder in rootFolders)
                {
                    var sourceDir = _fileSystem.Path.Combine(stagingPath, folder);
                    var targetDir = _fileSystem.Path.Combine(addOnsPath, folder);

                    if (HasIgnoreMarker(targetDir))
                    {
                        Log.Information("Skipping install of folder '{Folder}' because .ignoreme exists", folder);
                        continue;
                    }

                    if (_fileSystem.Directory.Exists(targetDir))
                    {
                        _fileSystem.Directory.Delete(targetDir, true);
                    }

                    _fileSystem.Directory.Move(sourceDir, targetDir);

                    var gitDir = _fileSystem.Path.Combine(targetDir, ".git");
                    if (!_fileSystem.Directory.Exists(gitDir))
                    {
                        _fileSystem.Directory.CreateDirectory(gitDir);
                    }
                }

                return rootFolders;
            }
        }
        finally
        {
            if (_fileSystem.Directory.Exists(stagingPath))
            {
                _fileSystem.Directory.Delete(stagingPath, true);
            }
        }
    }

    public virtual void UninstallAddOn(string name)
    {
        var folders = _config.InstalledAddOnFolders.GetValueOrDefault(name, [name]);
        foreach (var folder in folders)
        {
            var addOnPath = _fileSystem.Path.Combine(_config.AddOnsPath, folder);
            if (_fileSystem.Directory.Exists(addOnPath))
            {
                if (HasIgnoreMarker(addOnPath))
                {
                    Log.Information("Skipping uninstall of folder '{Folder}' because .ignoreme exists", folder);
                    continue;
                }
                _fileSystem.Directory.Delete(addOnPath, true);
                Log.Information("Uninstalled addon folder '{Folder}'", folder);
            }
        }
        _config.RemoveInstalledFolders(name);
    }

    private bool HasIgnoreMarker(string folderPath)
    {
        if (!_fileSystem.Directory.Exists(folderPath))
            return false;
        return _fileSystem.File.Exists(_fileSystem.Path.Combine(folderPath, ".ignoreme"));
    }
}