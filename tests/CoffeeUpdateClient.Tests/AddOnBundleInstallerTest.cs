using NUnit.Framework;
using CoffeeUpdateClient.Utils;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CoffeeUpdateClient.Tests.Mocks;
using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Tests;

public class AddOnBundleInstallerTest : ConfigTestBase
{
    private MockEnv _env = null!;
    private AddOnBundleInstaller _installer = null!;
    private string _addOnsPath = null!;

    [SetUp]
    public void SetUp()
    {
        _env = new MockEnv();
        _installer = new AddOnBundleInstaller(_env);
        _addOnsPath = Config.Instance.AddOnsPath;
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
    public void InstallAddOn_BundleWithIncorrectRootFolder_ThrowsInvalidOperationException()
    {
        var addOnName = "MyAddOn";
        var filesInZip = new[] { "file1.lua" };
        var addOnBundle = CreateBundle(addOnName, filesInZip, "WrongRootFolder");

        var ex = Assert.Throws<InvalidOperationException>(() => _installer.InstallAddOn(addOnBundle));
        Assert.That(ex?.Message, Does.Contain($"AddOn bundle for '{addOnName}' must contain a single root folder named '{addOnName}'. Found: WrongRootFolder"));
    }

    [Test]
    public void InstallAddOn_BundleWithMultipleRootFolders_ThrowsInvalidOperationException()
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            archive.CreateEntry("Root1/file.txt");
            archive.CreateEntry("Root2/file.txt");
        }
        memoryStream.Seek(0, SeekOrigin.Begin);
        
        var addOnName = "MyAddOn";
        var bundle = new AddOnBundle(new AddOnMetadata()
        {
            Name = addOnName,
            Version = "v1.0.0",
        }, memoryStream);
        var ex = Assert.Throws<InvalidOperationException>(() => _installer.InstallAddOn(bundle));
        Assert.That(ex?.Message, Does.Contain($"AddOn bundle for '{addOnName}' must contain a single root folder named '{addOnName}'. Found: Root1, Root2"));
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
        Assert.That(ex?.Message, Does.Contain($"AddOn bundle for '{addOnName}' must contain a single root folder named '{addOnName}'. Found: "));
    }
}
