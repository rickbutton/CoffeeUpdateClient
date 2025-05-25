using CoffeeUpdateClient.Utils;

namespace CoffeeUpdateClient.Tests;

[TestFixture]
class TOCParserTest
{
    [Test]
    public void GetVersion_ValidTOC_ReturnsVersion()
    {
        var input = @"## Interface: 110105
## Version: v9921
## Title: Aura Updater (with tweaks for <Coffee>)
## Author: Naemesis";

        var result = TOCParser.GetVersion(input);

        Assert.That(result, Is.EqualTo("v9921"));
    }

    [Test]
    public void GetVersion_ValidTOC_StripsWhitespace()
    {
        var input = @"## Interface: 110105
## Version: v9921    
## Title: Aura Updater (with tweaks for <Coffee>)
## Author: Naemesis";

        var result = TOCParser.GetVersion(input);

        Assert.That(result, Is.EqualTo("v9921"));
    }

    [Test]
    public void GetVersion_InvalidTOC_MissingVersion_ReturnsNull()
    {
        var input = @"## Interface: 110105
## Title: Aura Updater (with tweaks for <Coffee>)
## Author: Naemesis";

        var result = TOCParser.GetVersion(input);

        Assert.That(result, Is.EqualTo(null));
    }

    [Test]
    public void GetVersion_InvalidTOC_Empty_ReturnsNull()
    {
        var input = @"";

        var result = TOCParser.GetVersion(input);

        Assert.That(result, Is.EqualTo(null));
    }
}