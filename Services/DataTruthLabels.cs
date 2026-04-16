namespace AetherVault.Services;

/// <summary>
/// Human-readable labels for which MTG catalog and price snapshot the app is using (local-first "truth").
/// </summary>
public static class DataTruthLabels
{
    public static string FormatCatalogLine(string? versionOrTag)
    {
        if (string.IsNullOrWhiteSpace(versionOrTag))
            return "Card catalog: version unknown (update from Settings when online).";
        return $"Card catalog: {versionOrTag.Trim()}";
    }

    public static string FormatPricesLine(string? metaDate)
    {
        if (string.IsNullOrWhiteSpace(metaDate))
            return "Price data: not loaded or date unknown.";
        return $"Price data (bundle): {metaDate.Trim()}";
    }

    public static string HelpBody =>
        "Legality, oracle text, and card metadata follow the installed card database (shown as the catalog line). " +
        "When you download a newer catalog, those rules update for the whole app.\n\n" +
        "Prices come from the bundled MTGJSON-derived price file; the date is the bundle meta when the app saved it. " +
        "Deck lists and quantities always live on your device unless you export them.";
}
