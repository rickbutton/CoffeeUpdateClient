using CoffeeUpdateClient.Utils;

namespace CoffeeUpdateClient.Tests;

public class AddOnPathResolverTest
{
    [Test]
    public void NormalizeAddOnsDirectory_NullInput_ReturnsNull()
    {
        var result = AddOnPathResolver.NormalizeAddOnsDirectory(null);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_EmptyString_ReturnsNull()
    {
        var result = AddOnPathResolver.NormalizeAddOnsDirectory("");

        Assert.That(result, Is.Null);
    }    [Test]
    public void NormalizeAddOnsDirectory_WhitespaceString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => 
        {
            AddOnPathResolver.NormalizeAddOnsDirectory("   ");
        });
    }

    [Test]
    public void NormalizeAddOnsDirectory_ValidAddOnsPath_ReturnsUnchanged()
    {
        var addOnsPath = @"C:\Program Files (x86)\World of Warcraft\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(addOnsPath);

        Assert.That(result, Is.EqualTo(addOnsPath));
    }

    [Test]
    public void NormalizeAddOnsDirectory_ValidAddOnsPathRelative_ReturnsFullPath()
    {
        var addOnsPath = @"_retail_\Interface\AddOns";
        var expectedPath = Path.GetFullPath(addOnsPath);

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(addOnsPath);

        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizeAddOnsDirectory_InterfacePath_AppendsAddOns()
    {
        var interfacePath = @"C:\Program Files (x86)\World of Warcraft\_retail_\Interface";
        var expectedPath = @"C:\Program Files (x86)\World of Warcraft\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(interfacePath);

        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizeAddOnsDirectory_RetailPath_AppendsInterfaceAddOns()
    {
        var retailPath = @"C:\Program Files (x86)\World of Warcraft\_retail_";
        var expectedPath = @"C:\Program Files (x86)\World of Warcraft\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(retailPath);

        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizeAddOnsDirectory_WorldOfWarcraftPath_AppendsFullStructure()
    {
        var wowPath = @"C:\Program Files (x86)\World of Warcraft";
        var expectedPath = @"C:\Program Files (x86)\World of Warcraft\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(wowPath);

        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizeAddOnsDirectory_InvalidPath_ReturnsNull()
    {
        var invalidPath = @"C:\Some\Random\Path";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(invalidPath);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_InvalidGameDirectory_ReturnsNull()
    {
        var invalidPath = @"C:\Games\Some Other Game";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(invalidPath);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_AddOnsPathWithIncorrectParentStructure_ReturnsNull()
    {
        var incorrectPath = @"C:\Program Files (x86)\World of Warcraft\WrongParent\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(incorrectPath);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_InterfacePathWithIncorrectParent_ReturnsNull()
    {
        var incorrectPath = @"C:\Program Files (x86)\World of Warcraft\WrongParent\Interface";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(incorrectPath);

        Assert.That(result, Is.Null);
    }    [Test]
    public void NormalizeAddOnsDirectory_RetailPathWithIncorrectStructure_IsProcessedBasedOnFolderNameOnly()
    {
        var incorrectPath = @"C:\Program Files (x86)\SomeOtherGame\_retail_";
        var expectedPath = @"C:\Program Files (x86)\SomeOtherGame\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(incorrectPath);

        Assert.That(result, Is.EqualTo(expectedPath));
    }[Test]
    public void NormalizeAddOnsDirectory_PathWithTrailingSlash_ReturnsNull()
    {
        var pathWithSlash = @"C:\Program Files (x86)\World of Warcraft\";
        
        var result = AddOnPathResolver.NormalizeAddOnsDirectory(pathWithSlash);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_PathWithForwardSlashes_HandlesCorrectly()
    {
        var pathWithForwardSlashes = "C:/Program Files (x86)/World of Warcraft";
        var expectedPath = @"C:\Program Files (x86)\World of Warcraft\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(pathWithForwardSlashes);

        Assert.That(result, Is.EqualTo(expectedPath));
    }    [Test]
    public void NormalizeAddOnsDirectory_CaseDifferences_ReturnsNull()
    {
        var mixedCasePath = @"C:\Program Files (x86)\world of warcraft";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(mixedCasePath);

        Assert.That(result, Is.Null);
    }    [Test]
    public void NormalizeAddOnsDirectory_RetailCaseDifferences_ReturnsNull()
    {
        var mixedCasePath = @"C:\Program Files (x86)\World of Warcraft\_RETAIL_";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(mixedCasePath);

        Assert.That(result, Is.Null);
    }    [Test]
    public void NormalizeAddOnsDirectory_InterfaceCaseDifferences_ReturnsNull()
    {
        var mixedCasePath = @"C:\Program Files (x86)\World of Warcraft\_retail_\INTERFACE";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(mixedCasePath);

        Assert.That(result, Is.Null);
    }    [Test]
    public void NormalizeAddOnsDirectory_AddOnsCaseDifferences_ReturnsNull()
    {
        var mixedCasePath = @"C:\Program Files (x86)\World of Warcraft\_retail_\Interface\ADDONS";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(mixedCasePath);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_AlternativeInstallLocation_WorksCorrectly()
    {
        var altPath = @"D:\Games\World of Warcraft";
        var expectedPath = @"D:\Games\World of Warcraft\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(altPath);

        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizeAddOnsDirectory_CustomDirectoryName_ReturnsNull()
    {
        var customPath = @"C:\Program Files (x86)\WoW Custom";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(customPath);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_ClassicInsteadOfRetail_ReturnsNull()
    {
        var classicPath = @"C:\Program Files (x86)\World of Warcraft\_classic_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(classicPath);

        Assert.That(result, Is.Null);
    }    [Test]
    public void NormalizeAddOnsDirectory_NestedWorldOfWarcraftDirectories_IsProcessedBasedOnFolderNameOnly()
    {
        var nestedPath = @"C:\Program Files (x86)\World of Warcraft\Backup\World of Warcraft";
        var expectedPath = @"C:\Program Files (x86)\World of Warcraft\Backup\World of Warcraft\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(nestedPath);

        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizeAddOnsDirectory_PartialMatchInPath_ReturnsNull()
    {
        var partialPath = @"C:\Program Files (x86)\My World of Warcraft Game";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(partialPath);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_AltInterfacePathCombination_ReturnsNull()
    {
        var altPath = @"C:\Games\World of Warcraft\Interface";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(altPath);

        // This is null because it checks parent dir name which must be "_retail_"
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeAddOnsDirectory_AltRetailPathCombination_AppendsInterfaceAddOns()
    {
        var altPath = @"D:\WoW\_retail_";
        var expectedPath = @"D:\WoW\_retail_\Interface\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(altPath);

        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizeAddOnsDirectory_AddOnsPathWithoutProperParents_ReturnsNull()
    {
        var invalidPath = @"C:\Games\AddOns";

        var result = AddOnPathResolver.NormalizeAddOnsDirectory(invalidPath);

        Assert.That(result, Is.Null);
    }
}
