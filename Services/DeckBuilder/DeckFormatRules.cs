using AetherVault.Core;

namespace AetherVault.Services.DeckBuilder;

/// <summary>Static format → rule lookups for deck building and validation. Do not duplicate these switches in validators.</summary>
public static class DeckFormatRules
{
    /// <summary>Singleton-copy formats (commander identity, max 1 non-basic copy per name).</summary>
    public static bool IsCommanderLike(DeckFormat format) => format.IsCommanderLikeRules();

    /// <summary>Max copies of a non-basic, non-exception card with that name.</summary>
    public static int MaxNonBasicCopies(DeckFormat format) => IsCommanderLike(format) ? 1 : 4;

    /// <summary>Formats that use a 60+ minimum main and 15-card sideboard cap for validation warnings.</summary>
    public static bool UsesConstructedSideboardCap(DeckFormat format) =>
        format is DeckFormat.Standard or DeckFormat.Modern or DeckFormat.Pioneer or DeckFormat.Legacy or DeckFormat.Vintage
            or DeckFormat.Historic or DeckFormat.Timeless;

    /// <summary>Minimum main-deck cards for <see cref="UsesConstructedSideboardCap"/> formats; 0 if not applicable.</summary>
    public static int MinMainDeckCardsForConstructedWarning(DeckFormat format) =>
        UsesConstructedSideboardCap(format) ? DeckValidationConstants.MinMainConstructedDeck : 0;

    public static int CommanderLikeDeckTargetTotal => DeckValidationConstants.CommanderLikeDeckTargetCards;

    public static int MaxConstructedSideboardCards => DeckValidationConstants.MaxConstructedSideboardCards;
}
