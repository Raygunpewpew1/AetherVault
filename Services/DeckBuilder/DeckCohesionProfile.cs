namespace AetherVault.Services.DeckBuilder;

/// <summary>Aggregated theme data for main deck + commander (excludes sideboard).</summary>
public sealed class DeckCohesionProfile
{
    /// <summary>Subtype string (e.g. Elf) -> total quantity across deck slots.</summary>
    public Dictionary<string, int> SubtypeTotals { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Oracle keyword -> total quantity.</summary>
    public Dictionary<string, int> KeywordTotals { get; } = new(StringComparer.OrdinalIgnoreCase);
}
