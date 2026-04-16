namespace AetherVault.Tests.ImportExport;

public class MoxfieldDeckJsonParserTests
{
    [Fact]
    public void TryBuildRows_MinimalMainboard_ParsesQuantityAndIds()
    {
        const string json = """
            {"name":"Test Deck","format":"commander","boards":{"mainboard":{"cards":{"slot1":{"quantity":3,"card":{"name":"Mountain","set":"neo","cn":"295","scryfall_id":"00000000-0000-0000-0000-000000000001"}}}}}}
            """;

        Assert.True(MoxfieldDeckJsonParser.TryBuildRows(json, out var deckName, out var format, out var rows, out var error));
        Assert.Null(error);
        Assert.Equal("Test Deck", deckName);
        Assert.Equal("commander", format);
        Assert.Single(rows);
        Assert.Equal(3, rows[0].Quantity);
        Assert.Equal("Mountain", rows[0].CardName);
        Assert.Equal("neo", rows[0].SetCode);
        Assert.Equal("295", rows[0].CollectorNumber);
        Assert.Equal("00000000-0000-0000-0000-000000000001", rows[0].ScryfallId);
        Assert.Equal(DeckCsvV1.Sections.Main, rows[0].Section);
    }

    [Fact]
    public void TryParseMoxfieldDeckUrl_ExtractsPublicId()
    {
        Assert.True(DeckUrlImporter.TryParseMoxfieldDeckUrl(
            "https://www.moxfield.com/decks/oEWXWHM5eEGMmopExLWRCA",
            out var id));
        Assert.Equal("oEWXWHM5eEGMmopExLWRCA", id);
    }
}
