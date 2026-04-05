using System.IO;
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

        var tempExtractPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), $"{bundle.Metadata.Name}-Install-{Guid.NewGuid().ToString()}");
        _fileSystem.Directory.CreateDirectory(tempExtractPath);
        Log.Information("Extracting add-on bundle for '{AddOnName}' to temporary path: {TempPath}", bundle.Metadata.Name, tempExtractPath);

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
                    var fullPath = _fileSystem.Path.Combine(tempExtractPath, entry.FullName).Replace("/", _fileSystem.Path.DirectorySeparatorChar.ToString());
                    Log.Verbose("Processing entry: {EntryName} -> {FullPath}", entry.Name, fullPath);

                    if (entry.Name == "")
                    {
                        // entry is a directory
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
                    var sourceDir = _fileSystem.Path.Combine(tempExtractPath, folder);
                    var targetDir = _fileSystem.Path.Combine(addOnsPath, folder);

                    if (_fileSystem.Directory.Exists(targetDir))
                    {
                        _fileSystem.Directory.Delete(targetDir, true);
                    }

                    DeepCopy(sourceDir, targetDir);

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
            if (_fileSystem.Directory.Exists(tempExtractPath))
            {
                _fileSystem.Directory.Delete(tempExtractPath, true);
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
                _fileSystem.Directory.Delete(addOnPath, true);
                Log.Information("Uninstalled addon folder '{Folder}'", folder);
            }
        }
        _config.RemoveInstalledFolders(name);
    }

    private void DeepCopy(string sourceDir, string destinationDir)
    {
        if (!_fileSystem.Directory.Exists(destinationDir))
        {
            _fileSystem.Directory.CreateDirectory(destinationDir);
        }

        foreach (string dir in _fileSystem.Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            string dirToCreate = _fileSystem.Path.Combine(destinationDir, _fileSystem.Path.GetRelativePath(sourceDir, dir));
            _fileSystem.Directory.CreateDirectory(dirToCreate);
        }

        foreach (string newPath in _fileSystem.Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            string destPath = _fileSystem.Path.Combine(destinationDir, _fileSystem.Path.GetRelativePath(sourceDir, newPath));
            _fileSystem.File.Copy(newPath, destPath, true);
        }
    }
}