using CoffeeUpdateClient.Utils;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;

namespace CoffeeUpdateClient.Tests;

public class AddOnPathResolverTest
{
    [TestCase(null, TestName = "NullInput_ReturnsNull")]
    [TestCase("", TestName = "EmptyString_ReturnsNull")]
    [TestCase("    ", TestName = "EmptyString_ReturnsNull")]
    public void NormalizeAddOnsDirectory_NullOrEmptyInput_ReturnsNull(string? inputPath)
    {
        Assert.That(AddOnPathResolver.NormalizeAddOnsDirectory(inputPath), Is.Null);
    }

    [TestCase(@"C:\Program Files (x86)\World of Warcraft\_retail_\Interface\AddOns", TestName = "ValidAddOnsPath_ReturnsUnchanged")]
    public void NormalizeAddOnsDirectory_ValidAddOnsPaths(string addOnsPath)
    {
        var result = AddOnPathResolver.NormalizeAddOnsDirectory(addOnsPath);

        Assert.That(result, Is.EqualTo(addOnsPath)); // Expected path is the same as input for absolute paths
    }

    [TestCaseSource(nameof(ValidPathCases))]
    public void NormalizeAddOnsDirectory_ValidPaths(string inputPath, string expectedSuffix)
    {
        var result = AddOnPathResolver.NormalizeAddOnsDirectory(inputPath);
        var expectedPath = Path.Join(inputPath, expectedSuffix);
        Assert.That(result, Is.EqualTo(expectedPath));
    }

    private static IEnumerable<TestCaseData> ValidPathCases()
    {
        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\_retail_\Interface", "AddOns")
            .SetName("InterfacePath_AppendsAddOns");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\_retail_", @"Interface\AddOns")
            .SetName("RetailPath_AppendsInterfaceAddOns");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft", @"_retail_\Interface\AddOns")
            .SetName("WorldOfWarcraftPath_AppendsFullStructure");

        yield return new TestCaseData(@"D:\Games\World of Warcraft", @"_retail_\Interface\AddOns")
            .SetName("AlternativeInstallLocation_WorksCorrectly");

        yield return new TestCaseData(@"D:\WoW\_retail_", @"Interface\AddOns")
            .SetName("AltRetailPathCombination_AppendsInterfaceAddOns");

        yield return new TestCaseData(@"C:\Program Files (x86)\SomeOtherGame\_retail_", @"Interface\AddOns")
            .SetName("RetailPathWithIncorrectStructure_IsProcessedBasedOnFolderNameOnly");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\Backup\World of Warcraft", @"_retail_\Interface\AddOns")
            .SetName("NestedWorldOfWarcraftDirectories_IsProcessedBasedOnFolderNameOnly");
    }

    [TestCaseSource(nameof(InvalidPathCases))]
    public void NormalizeAddOnsDirectory_InvalidPaths_ReturnsNull(string invalidPath)
    {
        var result = AddOnPathResolver.NormalizeAddOnsDirectory(invalidPath);

        Assert.That(result, Is.Null);
    }

    private static IEnumerable<TestCaseData> InvalidPathCases()
    {
        yield return new TestCaseData(@"C:\Some\Random\Path")
            .SetName("InvalidPath_ReturnsNull");

        yield return new TestCaseData(@"C:\Games\Some Other Game")
            .SetName("InvalidGameDirectory_ReturnsNull");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\WrongParent\AddOns")
            .SetName("AddOnsPathWithIncorrectParentStructure_ReturnsNull");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\WrongParent\Interface")
            .SetName("InterfacePathWithIncorrectParent_ReturnsNull");

        yield return new TestCaseData(@"C:\Program Files (x86)\WoW Custom")
            .SetName("CustomDirectoryName_ReturnsNull");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\_classic_\Interface\AddOns")
            .SetName("ClassicInsteadOfRetail_ReturnsNull");

        yield return new TestCaseData(@"C:\Program Files (x86)\My World of Warcraft Game")
            .SetName("PartialMatchInPath_ReturnsNull");

        yield return new TestCaseData(@"C:\Games\World of Warcraft\Interface")
            .SetName("AltInterfacePathCombination_ReturnsNull");

        yield return new TestCaseData(@"C:\Games\AddOns")
            .SetName("AddOnsPathWithoutProperParents_ReturnsNull");
    }

    [TestCaseSource(nameof(CaseSensitivityCases))]
    public void NormalizeAddOnsDirectory_CaseSensitivityTests_ReturnsNull(string mixedCasePath)
    {
        var result = AddOnPathResolver.NormalizeAddOnsDirectory(mixedCasePath);

        Assert.That(result, Is.Null);
    }

    private static IEnumerable<TestCaseData> CaseSensitivityCases()
    {
        yield return new TestCaseData(@"C:\Program Files (x86)\world of warcraft")
            .SetName("CaseDifferences_ReturnsNull");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\_RETAIL_")
            .SetName("RetailCaseDifferences_ReturnsNull");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\_retail_\INTERFACE")
            .SetName("InterfaceCaseDifferences_ReturnsNull");

        yield return new TestCaseData(@"C:\Program Files (x86)\World of Warcraft\_retail_\Interface\ADDONS")
            .SetName("AddOnsCaseDifferences_ReturnsNull");
    }
}
