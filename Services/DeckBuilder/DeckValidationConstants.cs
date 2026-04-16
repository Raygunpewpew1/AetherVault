namespace AetherVault.Services.DeckBuilder;

/// <summary>Shared constants for deck validation messages and rule thresholds.</summary>
public static class DeckValidationConstants
{
    /// <summary>Max card names listed in a single color-identity warning before truncation.</summary>
    public const int MaxOffendingNamesInMessage = 5;

    /// <summary>Oracle text fragment used to detect "any number of cards named …" exceptions.</summary>
    public const string RelentlessOracleTextFragment = "A deck can have any number of cards named";

    /// <summary>Oracle text fragment for cards that may serve as commander outside the usual type line.</summary>
    public const string CanBeYourCommanderOracleTextFragment = "can be your commander";

    /// <summary>Target main + commander physical cards for singleton-copy formats (Commander / EDH).</summary>
    public const int CommanderLikeDeckTargetCards = 100;

    /// <summary>Minimum main-deck cards for traditional constructed formats (soft warning only).</summary>
    public const int MinMainConstructedDeck = 60;

    /// <summary>Maximum sideboard cards for Standard / Modern / etc. (soft warning only).</summary>
    public const int MaxConstructedSideboardCards = 15;
}

/// <summary>Canonical deck row section names (persisted in <c>DeckCards.Section</c>).</summary>
public static class DeckCardSections
{
    public const string Main = "Main";
    public const string Sideboard = "Sideboard";
    public const string Commander = "Commander";
    public const string Companion = "Companion";
}
