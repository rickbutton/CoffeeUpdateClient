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

    public bool InstallAddOn(AddOnBundle bundle)
    {
        var addOnsPath = Config.Instance.AddOnsPath;

        var tempExtractPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), $"{bundle.Metadata.Name}-Install-{Guid.NewGuid().ToString()}");
        _fileSystem.Directory.CreateDirectory(tempExtractPath);
        Log.Information("Extracting add-on bundle for '{AddOnName}' to temporary path: {TempPath}", bundle.Metadata.Name, tempExtractPath);

        bool success = false;
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
                    throw new InvalidOperationException($"AddOn bundle for '{bundle.Metadata.Name}' must contain a single root folder named '{bundle.Metadata.Name}'. Found: {string.Join(", ", rootEntries)}");
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

                var sourceAddOnDir = _fileSystem.Path.Combine(tempExtractPath, bundle.Metadata.Name);
                var targetAddOnDir = _fileSystem.Path.Combine(addOnsPath, bundle.Metadata.Name);

                if (_fileSystem.Directory.Exists(targetAddOnDir))
                {
                    _fileSystem.Directory.Delete(targetAddOnDir, true);
                }

                DeepCopy(sourceAddOnDir, targetAddOnDir);
                success = true;
            }
        }
        finally
        {
            if (_fileSystem.Directory.Exists(tempExtractPath))
            {
                _fileSystem.Directory.Delete(tempExtractPath, true);
            }
        }
        return success;
    }

    private void DeepCopy(string sourceDir, string destinationDir)
    {
        if (!_fileSystem.Directory.Exists(destinationDir))
        {
            _fileSystem.Directory.CreateDirectory(destinationDir);
        }

        foreach (string dir in _fileSystem.Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            string dirToCreate = dir.Replace(sourceDir, destinationDir);
            _fileSystem.Directory.CreateDirectory(dirToCreate);
        }

        foreach (string newPath in _fileSystem.Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            string destPath = newPath.Replace(sourceDir, destinationDir);
            _fileSystem.File.Copy(newPath, destPath, true);
        }
    }
}