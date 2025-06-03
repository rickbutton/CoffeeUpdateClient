using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using Serilog;

namespace CoffeeUpdateClient.Utils;

public class AddOnBundleInstaller
{
    private readonly IFileSystem _fileSystem;

    public AddOnBundleInstaller(IEnv env)
    {
        _fileSystem = env.FileSystem;
    }

    public void InstallAddOn(string addOnsPath, AddOnBundle bundle)
    {
        var tempExtractPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), $"{bundle.Metadata.Name}-Install-{Guid.NewGuid().ToString()}");
        _fileSystem.Directory.CreateDirectory(tempExtractPath);
        Log.Information("Extracting add-on bundle for '{AddOnName}' to temporary path: {TempPath}", bundle.Metadata.Name, tempExtractPath);

        try
        {
            using (var archive = new ZipArchive(bundle.Data, ZipArchiveMode.Read))
            {
                var rootEntries = archive.Entries
                    .Select(e => e.FullName.Split(['/', '\\'])[0])
                    .Distinct()
                    .ToList();

                if (rootEntries.Count != 1 || !string.Equals(rootEntries[0], bundle.Metadata.Name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Addon bundle for '{bundle.Metadata.Name}' must contain a single root folder named '{bundle.Metadata.Name}'. Found: {string.Join(", ", rootEntries)}");
                }

                foreach (var entry in archive.Entries)
                {
                    var fullPath = _fileSystem.Path.Combine(tempExtractPath, entry.FullName).Replace("/", _fileSystem.Path.DirectorySeparatorChar.ToString());
                    Log.Information("Processing entry: {EntryName} -> {FullPath}", entry.Name, fullPath);

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

                var sourceAddonDir = _fileSystem.Path.Combine(tempExtractPath, bundle.Metadata.Name);
                var targetAddonDir = _fileSystem.Path.Combine(addOnsPath, bundle.Metadata.Name);

                if (_fileSystem.Directory.Exists(targetAddonDir))
                {
                    _fileSystem.Directory.Delete(targetAddonDir, true);
                }

                _fileSystem.Directory.Move(sourceAddonDir, targetAddonDir);
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
}