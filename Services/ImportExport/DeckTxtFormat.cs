using System.Text.RegularExpressions;

namespace AetherVault.Services.ImportExport;

/// <summary>
/// Parses common plain-text deck lists (MTG Arena exports, Moxfield TXT, Cockatrice-style lines).
/// </summary>
public static class DeckTxtFormat
{
    private static readonly Regex QuantityPrefix = new(
        @"^\s*(\d+)\s*[xX]?\s+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ArenaTailParen = new(
        @"\s+\(([A-Za-z0-9]{1,8})\)\s+(\S+)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ArenaTailBracket = new(
        @"\s+\[([A-Za-z0-9]{1,8})\]\s+(\S+)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// One parsed card line (section is normalized to Main / Sideboard / Commander).
    /// </summary>
    public sealed record Line(int Quantity, string CardName, string? SetCode, string? CollectorNumber, string Section);

    /// <summary>
    /// Parses deck text into card lines. Empty lines and # comments are skipped.
    /// </summary>
    /// <param name="text">Full file contents.</param>
    /// <param name="deckNameFromMetadata">If the file contains a leading <c>Name: ...</c> line, this receives that value.</param>
    public static IReadOnlyList<Line> Parse(string text, out string? deckNameFromMetadata)
    {
        deckNameFromMetadata = null;
        var rows = new List<Line>();
        var section = DeckCsvV1.Sections.Main;
        var inTokensSection = false;
        var seenFirstCardOrSection = false;

        using var reader = new StringReader(text);
        string? raw;
        while ((raw = reader.ReadLine()) != null)
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            if (!seenFirstCardOrSection &&
                line.StartsWith("Name:", StringComparison.OrdinalIgnoreCase))
            {
                deckNameFromMetadata = line["Name:".Length..].Trim();
                if (deckNameFromMetadata.Length == 0)
                    deckNameFromMetadata = null;
                continue;
            }

            if (TryParseSectionHeader(line, out var newSection, out var isTokens))
            {
                seenFirstCardOrSection = true;
                inTokensSection = isTokens;
                section = newSection;
                continue;
            }

            if (inTokensSection)
                continue;

            seenFirstCardOrSection = true;

            var parsed = ParseCardLine(line, section);
            if (parsed != null)
                rows.Add(parsed);
        }

        return rows;
    }

    private static bool TryParseSectionHeader(string line, out string normalizedSection, out bool isTokensSection)
    {
        normalizedSection = DeckCsvV1.Sections.Main;
        isTokensSection = false;

        var key = line.TrimEnd(':').Trim();
        if (key.Length == 0)
            return false;

        // ManaBox / some exporters: [COMMANDER], [CREATURES], [LANDS], …
        if (key.Length >= 2 && key[0] == '[' && key[^1] == ']')
            key = key[1..^1].Trim();

        if (key.Length == 0)
            return false;

        if (key.Equals("Main", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Maindeck", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Main deck", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("MD", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Deck", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSection = DeckCsvV1.Sections.Main;
            return true;
        }

        if (key.Equals("Sideboard", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("SB", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSection = DeckCsvV1.Sections.Sideboard;
            return true;
        }

        if (key.Equals("Commander", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Commanders", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("CMD", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSection = DeckCsvV1.Sections.Commander;
            return true;
        }

        if (key.Equals("Companion", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSection = DeckCsvV1.Sections.Sideboard;
            return true;
        }

        if (key.Equals("Maybeboard", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Maybe", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSection = DeckCsvV1.Sections.Sideboard;
            return true;
        }

        if (key.Equals("Tokens", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Token", StringComparison.OrdinalIgnoreCase))
        {
            isTokensSection = true;
            return true;
        }

        // Typal groupings (ManaBox text export, etc.) — all mainboard in our deck model
        if (key.Equals("Planeswalkers", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Creatures", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Artifact", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Artifacts", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Instant", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Instants", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Sorcery", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Sorceries", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Enchantment", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Enchantments", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Land", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Lands", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Battles", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Battle", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Dungeon", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Dungeons", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Planeswalker", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Creature", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSection = DeckCsvV1.Sections.Main;
            return true;
        }

        return false;
    }

    internal static Line? ParseCardLine(string line, string defaultSection)
    {
        line = line.Trim();
        if (line.Length == 0)
            return null;

        var section = defaultSection;

        if (line.StartsWith("SB:", StringComparison.OrdinalIgnoreCase))
        {
            section = DeckCsvV1.Sections.Sideboard;
            line = line["SB:".Length..].Trim();
        }
        else if (line.StartsWith("SIDEBOARD:", StringComparison.OrdinalIgnoreCase))
        {
            section = DeckCsvV1.Sections.Sideboard;
            line = line["SIDEBOARD:".Length..].Trim();
        }
        else if (line.StartsWith("CMDR:", StringComparison.OrdinalIgnoreCase) ||
                 line.StartsWith("COMMANDER:", StringComparison.OrdinalIgnoreCase))
        {
            section = DeckCsvV1.Sections.Commander;
            var cut = line.StartsWith("CMDR:", StringComparison.OrdinalIgnoreCase) ? "CMDR:".Length : "COMMANDER:".Length;
            line = line[cut..].Trim();
        }

        // MTGO-style foil marker
        if (line.StartsWith("*F*", StringComparison.OrdinalIgnoreCase))
            line = line[3..].Trim();

        var qtyMatch = QuantityPrefix.Match(line);
        int quantity = 1;
        if (qtyMatch.Success)
        {
            int.TryParse(qtyMatch.Groups[1].Value, out quantity);
            line = line[qtyMatch.Length..].Trim();
        }

        if (quantity <= 0 || line.Length == 0)
            return null;

        string? setCode = null;
        string? collectorNumber = null;

        var tailMatch = ArenaTailParen.Match(line);
        if (!tailMatch.Success)
            tailMatch = ArenaTailBracket.Match(line);

        if (tailMatch.Success)
        {
            var namePart = line[..tailMatch.Index].Trim();
            if (namePart.Length > 0)
            {
                setCode = tailMatch.Groups[1].Value;
                collectorNumber = tailMatch.Groups[2].Value;
                line = namePart;
            }
        }

        section = DeckCsvV1.Sections.Normalize(section);
        return new Line(quantity, line, setCode, collectorNumber, section);
    }
}
