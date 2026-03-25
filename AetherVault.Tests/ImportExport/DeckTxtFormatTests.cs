using AetherVault.Services.ImportExport;

namespace AetherVault.Tests.ImportExport;

public class DeckTxtFormatTests
{
    [Fact]
    public void Parse_ArenaStyle_SetsSetAndNumber()
    {
        var txt = "4 Opt (XLN) 65\n";
        var lines = DeckTxtFormat.Parse(txt, out var meta);
        Assert.Null(meta);
        Assert.Single(lines);
        Assert.Equal(4, lines[0].Quantity);
        Assert.Equal("Opt", lines[0].CardName);
        Assert.Equal("XLN", lines[0].SetCode);
        Assert.Equal("65", lines[0].CollectorNumber);
        Assert.Equal("Main", lines[0].Section);
    }

    [Fact]
    public void Parse_SideboardHeader_SwitchesSection()
    {
        var txt = "2 Shock\nSideboard\n1 Negate\n";
        var lines = DeckTxtFormat.Parse(txt, out _);
        Assert.Equal(2, lines.Count);
        Assert.Equal("Shock", lines[0].CardName);
        Assert.Equal("Main", lines[0].Section);
        Assert.Equal("Negate", lines[1].CardName);
        Assert.Equal("Sideboard", lines[1].Section);
    }

    [Fact]
    public void Parse_NameHeader_ReturnsDeckNameMetadata()
    {
        var txt = "Name: Azorius Control\n1 Plains\n";
        var lines = DeckTxtFormat.Parse(txt, out var meta);
        Assert.Equal("Azorius Control", meta);
        Assert.Single(lines);
    }

    [Fact]
    public void Parse_CmdrPrefix_UsesCommanderSection()
    {
        var txt = "CMDR: 1 Kenrith, the Returned King\n";
        var lines = DeckTxtFormat.Parse(txt, out _);
        Assert.Single(lines);
        Assert.Equal("Commander", lines[0].Section);
        Assert.Equal(1, lines[0].Quantity);
    }

    [Fact]
    public void Parse_SbPrefix_LineIsSideboard()
    {
        var txt = "SB: 2 Naturalize\n";
        var lines = DeckTxtFormat.Parse(txt, out _);
        Assert.Single(lines);
        Assert.Equal(2, lines[0].Quantity);
        Assert.Equal("Sideboard", lines[0].Section);
    }

    [Fact]
    public void Parse_TokensSection_SkipsTokenLines()
    {
        var txt = "1 Bird\nTokens\n1 Bird Token\nMain\n1 Bird\n";
        var lines = DeckTxtFormat.Parse(txt, out _);
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal("Bird", l.CardName));
    }

    [Fact]
    public void Parse_CommanderThenDeck_ManaBoxPlainSections()
    {
        var txt = "Commander\n1 The Ur-Dragon\nDeck\n1 Sol Ring\n";
        var lines = DeckTxtFormat.Parse(txt, out _);
        Assert.Equal(2, lines.Count);
        Assert.Equal("The Ur-Dragon", lines[0].CardName);
        Assert.Equal("Commander", lines[0].Section);
        Assert.Equal("Sol Ring", lines[1].CardName);
        Assert.Equal("Main", lines[1].Section);
    }

    [Fact]
    public void Parse_BracketSections_ManaBoxExportStyle()
    {
        var txt = """
            [COMMANDER]
            1 The Ur-Dragon

            [CREATURES]
            1 Sol Ring

            [LANDS]
            3 Forest
            """;
        var lines = DeckTxtFormat.Parse(txt, out _);
        Assert.Equal(3, lines.Count);
        Assert.Equal("The Ur-Dragon", lines[0].CardName);
        Assert.Equal("Commander", lines[0].Section);
        Assert.Equal("Sol Ring", lines[1].CardName);
        Assert.Equal("Main", lines[1].Section);
        Assert.Equal(3, lines[2].Quantity);
        Assert.Equal("Forest", lines[2].CardName);
        Assert.Equal("Main", lines[2].Section);
    }

    [Fact]
    public void Parse_ArenaSetTail_MultiQty_PlListCollector_SideboardHeader()
    {
        var txt = """
            1 Krenko, Mob Boss (CMM) 238
            33 Mountain (ECL) 282
            1 Rummaging Goblin (PLST) XLN-160
            1 Seize the Spotlight (SLD) 7044

            SIDEBOARD:
            1 Battle Squadron (EMA) 118
            1 Empty the Warrens (MMA) 112
            """;
        var lines = DeckTxtFormat.Parse(txt, out _);
        Assert.Equal(6, lines.Count);

        Assert.Equal("Krenko, Mob Boss", lines[0].CardName);
        Assert.Equal("CMM", lines[0].SetCode);
        Assert.Equal("238", lines[0].CollectorNumber);
        Assert.Equal("Main", lines[0].Section);

        Assert.Equal(33, lines[1].Quantity);
        Assert.Equal("Mountain", lines[1].CardName);
        Assert.Equal("ECL", lines[1].SetCode);
        Assert.Equal("282", lines[1].CollectorNumber);

        Assert.Equal("Rummaging Goblin", lines[2].CardName);
        Assert.Equal("PLST", lines[2].SetCode);
        Assert.Equal("XLN-160", lines[2].CollectorNumber);

        Assert.Equal("7044", lines[3].CollectorNumber);
        Assert.Equal("Main", lines[3].Section);

        Assert.Equal("Battle Squadron", lines[4].CardName);
        Assert.Equal("Sideboard", lines[4].Section);
        Assert.Equal("EMA", lines[4].SetCode);

        Assert.Equal("Empty the Warrens", lines[5].CardName);
        Assert.Equal("Sideboard", lines[5].Section);
        Assert.Equal("MMA", lines[5].SetCode);
    }
}
