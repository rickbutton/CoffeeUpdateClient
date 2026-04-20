using NUnit.Framework;
using CoffeeUpdater.Utils;
using System.IO;
using System.IO.Compression;
using System.Text;
using CoffeeUpdater.Tests.Mocks;
using CoffeeUpdater.Models;

namespace CoffeeUpdater.Tests;

public class AddOnBundleInstallerTest
{
    private MockEnv _env = null!;
    private AddOnBundleInstaller _installer = null!;
    private string _addOnsPath = null!;
    private Config _config = null!;

    [SetUp]
    public void SetUp()
    {
        _env = new MockEnv();
        _addOnsPath = @"C:\World of Warcraft\_retail_\Interface\AddOns";
        _config = new Config { AddOnsPath = _addOnsPath };
        _installer = new AddOnBundleInstaller(_env, _config);
        _env.FileSystem.Directory.CreateDirectory(_addOnsPath);
    }

    private AddOnBundle CreateBundle(string addOnName, string[] fileNames, string? rootFolderName = null)
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var actualRoot = rootFolderName ?? addOnName;
            foreach (var fileName in fileNames)
            {
                var entry = archive.CreateEntry(_env.FileSystem.Path.Combine(actualRoot, fileName));
                using (var entryStream = entry.Open())
                using (var streamWriter = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    streamWriter.Write($"Content of {fileName}");
                }
            }
        }
        memoryStream.Seek(0, SeekOrigin.Begin);

        var metadata = new AddOnMetadata();
        metadata.Name = addOnName;
        metadata.Version = "v1.0.0";

        return new AddOnBundle(metadata, memoryStream);
    }

    private AddOnBundle CreateMultiFolderBundle(string primaryName, Dictionary<string, string[]> folderFiles)
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var (folder, files) in folderFiles)
            {
                foreach (var fileName in files)
                {
                    var entry = archive.CreateEntry($"{folder}/{fileName}");
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.Write($"Content of {folder}/{fileName}");
                }
            }
        }
        memoryStream.Seek(0, SeekOrigin.Begin);

        return new AddOnBundle(new AddOnMetadata { Name = primaryName, Version = "v1.0.0" }, memoryStream);
    }

    [Test]
    public void InstallAddOn_ValidBundle_ExtractsFiles()
    {
        var addOnName = "MyAddOn";
        var filesInZip = new[] { "file1.lua", "subdir/file2.txt" };
        var addOnBundle = CreateBundle(addOnName, filesInZip);

        _installer.InstallAddOn(addOnBundle);

        var expectedAddOnPath = _env.FileSystem.Path.Combine(_addOnsPath, addOnName);
        Assert.That(_env.FileSystem.Directory.Exists(expectedAddOnPath), Is.True);
        foreach (var file in filesInZip)
        {
            var expectedFilePath = _env.FileSystem.Path.Combine(expectedAddOnPath, file);
            Assert.That(_env.FileSystem.File.Exists(expectedFilePath), Is.True, $"Expected file '{expectedFilePath}' not found.");
            Assert.That(_env.FileSystem.File.ReadAllText(expectedFilePath), Is.EqualTo($"Content of {file}"));
        }
    }

    [Test]
    public void InstallAddOn_ValidBundle_ReturnsInstalledFolders()
    {
        var addOnName = "MyAddOn";
        var result = _installer.InstallAddOn(CreateBundle(addOnName, ["file1.lua"]));
        Assert.That(result, Is.EqualTo(new[] { addOnName }));
    }

    [Test]
    public void InstallAddOn_OverwritesExistingFiles()
    {
        var addOnName = "MyAddOn";
        var filesInZip = new[] { "file1.lua" };

        // Create an existing file
        var targetAddOnDir = _env.FileSystem.Path.Combine(_addOnsPath, addOnName);
        _env.FileSystem.Directory.CreateDirectory(targetAddOnDir);
        var existingFilePath = _env.FileSystem.Path.Combine(targetAddOnDir, "file1.lua");
        _env.FileSystem.File.WriteAllText(existingFilePath, "Old content");

        var addOnBundle = CreateBundle(addOnName, filesInZip);

        _installer.InstallAddOn(addOnBundle);

        Assert.That(_env.FileSystem.File.ReadAllText(existingFilePath), Is.EqualTo("Content of file1.lua"));
    }

    [Test]
    public void InstallAddOn_DeletesExtraFilesNotInBundle()
    {
        var addOnName = "MyAddOn";
        var filesInZip = new[] { "file1.lua" };

        var targetAddOnDir = _env.FileSystem.Path.Combine(_addOnsPath, addOnName);
        _env.FileSystem.Directory.CreateDirectory(targetAddOnDir);
        var extraFilePath = _env.FileSystem.Path.Combine(targetAddOnDir, "extraFile.txt");
        _env.FileSystem.File.WriteAllText(extraFilePath, "This file should be deleted");
        var extraFileInSubdirPath = _env.FileSystem.Path.Combine(targetAddOnDir, "subdir", "extra2.txt");
        _env.FileSystem.Directory.CreateDirectory(_env.FileSystem.Path.GetDirectoryName(extraFileInSubdirPath)!);
        _env.FileSystem.File.WriteAllText(extraFileInSubdirPath, "This file should also be deleted");


        var addOnBundle = CreateBundle(addOnName, filesInZip);
        _installer.InstallAddOn(addOnBundle);

        Assert.That(_env.FileSystem.File.Exists(extraFilePath), Is.False, "Extra file was not deleted.");
        Assert.That(_env.FileSystem.File.Exists(extraFileInSubdirPath), Is.False, "Extra file in subdirectory was not deleted.");
        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.GetDirectoryName(extraFileInSubdirPath)), Is.False, "Extra subdirectory was not deleted.");
    }

    [Test]
    public void InstallAddOn_BundleWithNameNotInRootFolders_ThrowsInvalidOperationException()
    {
        var addOnName = "MyAddOn";
        var filesInZip = new[] { "file1.lua" };
        var addOnBundle = CreateBundle(addOnName, filesInZip, "WrongRootFolder");

        var ex = Assert.Throws<InvalidOperationException>(() => _installer.InstallAddOn(addOnBundle));
        Assert.That(ex?.Message, Does.Contain($"AddOn bundle for '{addOnName}' must contain a root folder named '{addOnName}' for version detection. Found: WrongRootFolder"));
    }

    [Test]
    public void InstallAddOn_MultiFolderBundle_ExtractsAllFolders()
    {
        var bundle = CreateMultiFolderBundle("BigWigs", new Dictionary<string, string[]>
        {
            ["BigWigs"] = ["BigWigs.toc", "Core.lua"],
            ["BigWigs_Options"] = ["Options.toc"],
            ["BigWigs_Plugins"] = ["Plugins.toc"],
        });

        var result = _installer.InstallAddOn(bundle);

        Assert.That(result, Is.EquivalentTo(new[] { "BigWigs", "BigWigs_Options", "BigWigs_Plugins" }));
        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs")), Is.True);
        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Options")), Is.True);
        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Plugins")), Is.True);
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs", "BigWigs.toc")), Is.True);
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Options", "Options.toc")), Is.True);
    }

    [Test]
    public void InstallAddOn_MultiFolderBundle_OverwritesExistingFolders()
    {
        // Pre-create old versions of all three folders
        foreach (var folder in new[] { "BigWigs", "BigWigs_Options", "BigWigs_Plugins" })
        {
            var dir = _env.FileSystem.Path.Combine(_addOnsPath, folder);
            _env.FileSystem.Directory.CreateDirectory(dir);
            _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(dir, "old.lua"), "old content");
        }

        var bundle = CreateMultiFolderBundle("BigWigs", new Dictionary<string, string[]>
        {
            ["BigWigs"] = ["BigWigs.toc"],
            ["BigWigs_Options"] = ["Options.toc"],
            ["BigWigs_Plugins"] = ["Plugins.toc"],
        });

        _installer.InstallAddOn(bundle);

        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs", "old.lua")), Is.False);
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs", "BigWigs.toc")), Is.True);
    }

    [Test]
    public void InstallAddOn_BundleWithExplicitDirectoryEntry_ExtractsCorrectly()
    {
        var addOnName = "MyAddOn";
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Explicit directory entry — exercises the entry.Name == "" branch
            archive.CreateEntry($"{addOnName}/");
            // File in that same directory — directoryPath already exists, exercises the false branch
            var fileEntry = archive.CreateEntry($"{addOnName}/file.lua");
            using var entryStream = fileEntry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            writer.Write("lua content");
        }
        memoryStream.Seek(0, SeekOrigin.Begin);

        var bundle = new AddOnBundle(new AddOnMetadata { Name = addOnName, Version = "1.0.0" }, memoryStream);
        _installer.InstallAddOn(bundle);

        var expectedFilePath = _env.FileSystem.Path.Combine(_addOnsPath, addOnName, "file.lua");
        Assert.That(_env.FileSystem.File.Exists(expectedFilePath), Is.True);
        Assert.That(_env.FileSystem.File.ReadAllText(expectedFilePath), Is.EqualTo("lua content"));
    }

    [Test]
    public void UninstallAddOn_DirectoryExists_DeletesDirectory()
    {
        var addOnDir = _env.FileSystem.Path.Combine(_addOnsPath, "OldAddOn");
        _env.FileSystem.Directory.CreateDirectory(addOnDir);
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(addOnDir, "file.lua"), "content");

        _installer.UninstallAddOn("OldAddOn");

        Assert.That(_env.FileSystem.Directory.Exists(addOnDir), Is.False);
    }

    [Test]
    public void UninstallAddOn_DirectoryDoesNotExist_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _installer.UninstallAddOn("NonExistent"));
    }

    [Test]
    public void UninstallAddOn_WithTrackedFolders_DeletesAllFolders()
    {
        _config.SetInstalledFolders("BigWigs", ["BigWigs", "BigWigs_Options", "BigWigs_Plugins"]);

        foreach (var folder in new[] { "BigWigs", "BigWigs_Options", "BigWigs_Plugins" })
        {
            var dir = _env.FileSystem.Path.Combine(_addOnsPath, folder);
            _env.FileSystem.Directory.CreateDirectory(dir);
            _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(dir, "file.lua"), "content");
        }

        _installer.UninstallAddOn("BigWigs");

        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs")), Is.False);
        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Options")), Is.False);
        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Plugins")), Is.False);
    }

    [Test]
    public void UninstallAddOn_WithTrackedFolders_RemovesConfigEntry()
    {
        _config.SetInstalledFolders("BigWigs", ["BigWigs", "BigWigs_Options"]);

        _installer.UninstallAddOn("BigWigs");

        Assert.That(_config.InstalledAddOnFolders.ContainsKey("BigWigs"), Is.False);
    }

    [Test]
    public void InstallAddOn_CreatesGitDirectoryInEachFolder()
    {
        var bundle = CreateMultiFolderBundle("BigWigs", new Dictionary<string, string[]>
        {
            ["BigWigs"] = ["BigWigs.toc"],
            ["BigWigs_Options"] = ["Options.toc"],
        });

        _installer.InstallAddOn(bundle);

        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs", ".git")), Is.True);
        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Options", ".git")), Is.True);
    }

    [Test]
    public void InstallAddOn_CleansStagingDirectoryAfterInstall()
    {
        var bundle = CreateBundle("MyAddOn", ["file1.lua"]);

        _installer.InstallAddOn(bundle);

        var interfacePath = _env.FileSystem.Path.GetDirectoryName(_addOnsPath)!;
        var stagingDir = _env.FileSystem.Path.Combine(interfacePath, "CoffeeUpdaterStaging");
        if (_env.FileSystem.Directory.Exists(stagingDir))
        {
            var remaining = _env.FileSystem.Directory.GetDirectories(stagingDir);
            Assert.That(remaining, Is.Empty, "Staging directory should be empty after install");
        }
    }

    [Test]
    public void InstallAddOn_IgnoreMePresent_SkipsFolderAndPreservesUserFiles()
    {
        var addOnName = "MyAddOn";
        var targetDir = _env.FileSystem.Path.Combine(_addOnsPath, addOnName);
        _env.FileSystem.Directory.CreateDirectory(targetDir);
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(targetDir, ".ignoreme"), "");
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(targetDir, "dev.lua"), "local dev content");

        var bundle = CreateBundle(addOnName, ["file1.lua"]);
        var result = _installer.InstallAddOn(bundle);

        Assert.That(result, Is.EqualTo(new[] { addOnName }));
        Assert.That(_env.FileSystem.File.ReadAllText(_env.FileSystem.Path.Combine(targetDir, "dev.lua")), Is.EqualTo("local dev content"));
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(targetDir, "file1.lua")), Is.False);
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(targetDir, ".ignoreme")), Is.True);
    }

    [Test]
    public void InstallAddOn_MultiFolderBundle_IgnoreMeInOneFolder_SkipsOnlyThatFolder()
    {
        foreach (var folder in new[] { "BigWigs", "BigWigs_Options", "BigWigs_Plugins" })
        {
            var dir = _env.FileSystem.Path.Combine(_addOnsPath, folder);
            _env.FileSystem.Directory.CreateDirectory(dir);
            _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(dir, "old.lua"), $"old {folder}");
        }

        var ignoredDir = _env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Options");
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(ignoredDir, ".ignoreme"), "");
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(ignoredDir, "dev.lua"), "dev work");

        var bundle = CreateMultiFolderBundle("BigWigs", new Dictionary<string, string[]>
        {
            ["BigWigs"] = ["BigWigs.toc", "Core.lua"],
            ["BigWigs_Options"] = ["Options.toc"],
            ["BigWigs_Plugins"] = ["Plugins.toc"],
        });

        var result = _installer.InstallAddOn(bundle);

        Assert.That(result, Is.EquivalentTo(new[] { "BigWigs", "BigWigs_Options", "BigWigs_Plugins" }));

        var bigWigsDir = _env.FileSystem.Path.Combine(_addOnsPath, "BigWigs");
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(bigWigsDir, "BigWigs.toc")), Is.True);
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(bigWigsDir, "old.lua")), Is.False);

        Assert.That(_env.FileSystem.File.ReadAllText(_env.FileSystem.Path.Combine(ignoredDir, "dev.lua")), Is.EqualTo("dev work"));
        Assert.That(_env.FileSystem.File.ReadAllText(_env.FileSystem.Path.Combine(ignoredDir, "old.lua")), Is.EqualTo("old BigWigs_Options"));
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(ignoredDir, "Options.toc")), Is.False);
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(ignoredDir, ".ignoreme")), Is.True);

        var pluginsDir = _env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Plugins");
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(pluginsDir, "Plugins.toc")), Is.True);
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(pluginsDir, "old.lua")), Is.False);
    }

    [Test]
    public void InstallAddOn_IgnoreMePresent_DoesNotCreateGitDirectory()
    {
        var addOnName = "MyAddOn";
        var targetDir = _env.FileSystem.Path.Combine(_addOnsPath, addOnName);
        _env.FileSystem.Directory.CreateDirectory(targetDir);
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(targetDir, ".ignoreme"), "");

        _installer.InstallAddOn(CreateBundle(addOnName, ["file1.lua"]));

        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(targetDir, ".git")), Is.False);
    }

    [Test]
    public void UninstallAddOn_IgnoreMePresent_PreservesFolder()
    {
        var addOnDir = _env.FileSystem.Path.Combine(_addOnsPath, "MyAddOn");
        _env.FileSystem.Directory.CreateDirectory(addOnDir);
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(addOnDir, ".ignoreme"), "");
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(addOnDir, "dev.lua"), "dev");

        _installer.UninstallAddOn("MyAddOn");

        Assert.That(_env.FileSystem.Directory.Exists(addOnDir), Is.True);
        Assert.That(_env.FileSystem.File.ReadAllText(_env.FileSystem.Path.Combine(addOnDir, "dev.lua")), Is.EqualTo("dev"));
    }

    [Test]
    public void UninstallAddOn_MultiFolderBundle_IgnoreMeInOneFolder_PreservesOnlyThatFolder()
    {
        _config.SetInstalledFolders("BigWigs", ["BigWigs", "BigWigs_Options", "BigWigs_Plugins"]);

        foreach (var folder in new[] { "BigWigs", "BigWigs_Options", "BigWigs_Plugins" })
        {
            var dir = _env.FileSystem.Path.Combine(_addOnsPath, folder);
            _env.FileSystem.Directory.CreateDirectory(dir);
            _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(dir, "file.lua"), "content");
        }

        var ignoredDir = _env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Plugins");
        _env.FileSystem.File.WriteAllText(_env.FileSystem.Path.Combine(ignoredDir, ".ignoreme"), "");

        _installer.UninstallAddOn("BigWigs");

        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs")), Is.False);
        Assert.That(_env.FileSystem.Directory.Exists(_env.FileSystem.Path.Combine(_addOnsPath, "BigWigs_Options")), Is.False);
        Assert.That(_env.FileSystem.Directory.Exists(ignoredDir), Is.True);
        Assert.That(_env.FileSystem.File.Exists(_env.FileSystem.Path.Combine(ignoredDir, "file.lua")), Is.True);
    }

    [Test]
    public void InstallAddOn_EmptyBundle_ThrowsInvalidOperationException()
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // No entries
        }
        memoryStream.Seek(0, SeekOrigin.Begin);

        var addOnName = "MyAddOn";
        var bundle = new AddOnBundle(new AddOnMetadata()
        {
            Name = addOnName,
            Version = "v1.0.0",
        }, memoryStream);
        var ex = Assert.Throws<InvalidOperationException>(() => _installer.InstallAddOn(bundle));
        Assert.That(ex?.Message, Does.Contain($"AddOn bundle for '{addOnName}' must contain a root folder named '{addOnName}' for version detection. Found: "));
    }
}
