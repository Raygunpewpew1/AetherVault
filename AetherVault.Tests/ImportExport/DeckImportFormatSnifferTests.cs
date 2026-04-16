using System.Text;

namespace AetherVault.Tests.ImportExport;

public class DeckImportFormatSnifferTests
{
    [Fact]
    public void DetectFormat_NoExtension_ArenaLine_IsTxt()
    {
        var ms = new MemoryStream(Encoding.UTF8.GetBytes("4 Lightning Bolt\n"));
        Assert.Equal(DeckImportFormatSniffer.DeckImportKind.Txt, DeckImportFormatSniffer.DetectFormat(null, ms));
    }

    [Fact]
    public void DetectFormat_NoExtension_AetherVaultCsvHeader_IsCsv()
    {
        var ms = new MemoryStream(Encoding.UTF8.GetBytes("Source,Deck Name,Format,Section,Quantity\n"));
        Assert.Equal(DeckImportFormatSniffer.DeckImportKind.Csv, DeckImportFormatSniffer.DetectFormat(null, ms));
    }

    [Fact]
    public void DetectFormat_NoExtension_SkipsCommentsAndBlankBeforeHeader()
    {
        var ms = new MemoryStream(Encoding.UTF8.GetBytes("\n# comment\n\nSource,Deck Name\n"));
        Assert.Equal(DeckImportFormatSniffer.DeckImportKind.Csv, DeckImportFormatSniffer.DetectFormat(null, ms));
    }

    [Fact]
    public void DetectFormat_TxtExtension_ForcesTxt_EvenIfFirstLineLooksLikeCsv()
    {
        // Malformed file: user named it .txt but content is CSV — honor extension.
        var ms = new MemoryStream(Encoding.UTF8.GetBytes("Source,Deck Name\n"));
        Assert.Equal(DeckImportFormatSniffer.DeckImportKind.Txt, DeckImportFormatSniffer.DetectFormat("x.txt", ms));
    }

    [Fact]
    public void DetectFormat_CsvExtension_ForcesCsv()
    {
        var ms = new MemoryStream(Encoding.UTF8.GetBytes("4 Opt\n"));
        Assert.Equal(DeckImportFormatSniffer.DeckImportKind.Csv, DeckImportFormatSniffer.DetectFormat("d.csv", ms));
    }
}
