using AetherVault.Core;

namespace AetherVault.ViewModels;

/// <summary>One-tap deck theme filter for the add-cards search (subtype or oracle keyword).</summary>
public sealed class DeckSynergyChipItem
{
    public required string DisplayText { get; init; }

    public required string SubtypeOrKeywordValue { get; init; }

    public required bool IsSubtype { get; init; }

    public SearchOptions ToPresetSearchOptions() => new()
    {
        PrimarySideOnly = true,
        SubtypeFilter = IsSubtype ? SubtypeOrKeywordValue : "",
        KeywordsFilter = IsSubtype ? "" : SubtypeOrKeywordValue,
    };
}
