using AetherVault.Models;

namespace AetherVault.Services.DeckBuilder;

/// <summary>
/// Heuristic role tags for deck statistics (not rules validation). Kept conservative and testable.
/// </summary>
public static class DeckCardRoleClassifier
{
    /// <summary>Returns how many copies of this card count toward each role bucket (0 or qty per flag).</summary>
    public static void AddRoleCounts(Card card, int quantity, DeckStats stats)
    {
        if (quantity <= 0) return;
        if (card.IsBasicLand) return;

        string text = card.Text ?? "";
        string lower = text.ToLowerInvariant();

        if (IsRamp(card, lower))
            stats.RampCount += quantity;
        if (IsCardDraw(lower))
            stats.CardDrawCount += quantity;
        if (IsRemoval(lower))
            stats.RemovalCount += quantity;
        if (IsBoardWipe(lower))
            stats.BoardWipesCount += quantity;
        if (IsCounterspell(lower))
            stats.CounterspellsCount += quantity;
    }

    internal static bool IsRamp(Card card, string textLower)
    {
        if (HasOracleKeyword(card, "Ramp"))
            return true;
        if (card.IsLand)
            return false;
        if (!string.IsNullOrEmpty(card.ProducedMana))
            return true;
        return textLower.Contains("search your library for a land", StringComparison.Ordinal)
               || textLower.Contains("put a land", StringComparison.Ordinal)
               || textLower.Contains("put up to two target land", StringComparison.Ordinal);
    }

    internal static bool IsCardDraw(string textLower) =>
        textLower.Contains("draw a card", StringComparison.Ordinal)
        || textLower.Contains("draw two cards", StringComparison.Ordinal)
        || textLower.Contains("draw three cards", StringComparison.Ordinal)
        || textLower.Contains("draw four cards", StringComparison.Ordinal)
        || textLower.Contains("draw cards equal", StringComparison.Ordinal)
        || textLower.Contains("draw x cards", StringComparison.Ordinal)
        || textLower.Contains("draw that many cards", StringComparison.Ordinal)
        || textLower.Contains("investigate", StringComparison.Ordinal);

    internal static bool IsRemoval(string textLower) =>
        textLower.Contains("destroy target creature", StringComparison.Ordinal)
        || textLower.Contains("exile target creature", StringComparison.Ordinal)
        || textLower.Contains("destroy target permanent", StringComparison.Ordinal)
        || textLower.Contains("exile target permanent", StringComparison.Ordinal)
        || textLower.Contains("destroy target creature or planeswalker", StringComparison.Ordinal)
        || textLower.Contains("exile target creature or planeswalker", StringComparison.Ordinal);

    internal static bool IsBoardWipe(string textLower) =>
        textLower.Contains("destroy all creatures", StringComparison.Ordinal)
        || textLower.Contains("destroy all nonland permanents", StringComparison.Ordinal)
        || textLower.Contains("destroy all artifacts", StringComparison.Ordinal)
        || textLower.Contains("destroy all enchantments", StringComparison.Ordinal)
        || textLower.Contains("exile all creatures", StringComparison.Ordinal);

    internal static bool IsCounterspell(string textLower) =>
        textLower.Contains("counter target spell", StringComparison.Ordinal)
        || textLower.Contains("counter target activated ability", StringComparison.Ordinal);

    private static bool HasOracleKeyword(Card card, string keyword) =>
        card.GetKeywordsArray().Contains(keyword, StringComparer.OrdinalIgnoreCase);

    // ── Public hints for deck suggestion scoring (same rules as role counts) ──

    public static bool CardMatchesRampHeuristic(Card card) =>
        IsRamp(card, (card.Text ?? "").ToLowerInvariant());

    public static bool CardMatchesCardDrawHeuristic(Card card) =>
        IsCardDraw((card.Text ?? "").ToLowerInvariant());

    public static bool CardMatchesRemovalHeuristic(Card card) =>
        IsRemoval((card.Text ?? "").ToLowerInvariant());

    public static bool CardMatchesBoardWipeHeuristic(Card card) =>
        IsBoardWipe((card.Text ?? "").ToLowerInvariant());

    public static bool CardMatchesCounterspellHeuristic(Card card) =>
        IsCounterspell((card.Text ?? "").ToLowerInvariant());
}
