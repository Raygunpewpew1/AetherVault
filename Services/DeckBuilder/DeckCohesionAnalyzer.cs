using AetherVault.Models;

namespace AetherVault.Services.DeckBuilder;

/// <summary>
/// Subtype/keyword aggregation and deck role counts for synergy UI.
/// </summary>
public static class DeckCohesionAnalyzer
{
    private static readonly string[] SubtypeNoise =
    [
        "Creature", "Artifact", "Enchantment", "Instant", "Sorcery", "Land", "Planeswalker", "Battle", "Tribal", "Kindred", "Legendary", "World", "Basic", "Snow", "Ongoing", "Dungeon", "Emblem", "Card"
    ];

    /// <summary>Main + Commander sections only.</summary>
    public static bool IncludeEntityForCohesion(DeckCardEntity e) =>
        e.Section is "Main" or "Commander";

    /// <summary>Fills role counters on <paramref name="stats"/> (curve/type totals unchanged).</summary>
    public static DeckCohesionProfile BuildProfileAndRoles(
        IEnumerable<DeckCardEntity> entities,
        IReadOnlyDictionary<string, Card> cardMap,
        DeckStats stats)
    {
        var profile = new DeckCohesionProfile();

        foreach (var entity in entities)
        {
            if (!IncludeEntityForCohesion(entity)) continue;
            if (!cardMap.TryGetValue(entity.CardId, out var card)) continue;
            if (card.IsBasicLand) continue;

            int qty = entity.Quantity;
            foreach (var st in card.GetSubtypesArray())
            {
                if (string.IsNullOrWhiteSpace(st)) continue;
                if (IsNoiseSubtype(st)) continue;
                profile.SubtypeTotals[st] = profile.SubtypeTotals.GetValueOrDefault(st) + qty;
            }

            foreach (var kw in card.GetKeywordsArray())
            {
                if (string.IsNullOrWhiteSpace(kw)) continue;
                profile.KeywordTotals[kw] = profile.KeywordTotals.GetValueOrDefault(kw) + qty;
            }

            DeckCardRoleClassifier.AddRoleCounts(card, qty, stats);
        }

        return profile;
    }

    /// <summary>Top entries by count, stable sort by label.</summary>
    public static IReadOnlyList<(string Label, int Count)> TopSubtypes(DeckCohesionProfile profile, int max = 8) =>
        TopEntries(profile.SubtypeTotals, max);

    /// <summary>Top oracle keywords by weighted count.</summary>
    public static IReadOnlyList<(string Label, int Count)> TopKeywords(DeckCohesionProfile profile, int max = 8) =>
        TopEntries(profile.KeywordTotals, max);

    private static IReadOnlyList<(string Label, int Count)> TopEntries(Dictionary<string, int> map, int max)
    {
        if (map.Count == 0) return [];
        return map
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    private static bool IsNoiseSubtype(string st) =>
        SubtypeNoise.Contains(st, StringComparer.OrdinalIgnoreCase);

    /// <summary>Weighted overlap with deck themes (other cards only).</summary>
    public static string? FormatOverlapHint(Card card, IEnumerable<DeckCardEntity> entities, IReadOnlyDictionary<string, Card> cardMap)
    {
        var cardSub = new HashSet<string>(card.GetSubtypesArray(), StringComparer.OrdinalIgnoreCase);
        foreach (var s in cardSub.ToArray())
            if (IsNoiseSubtype(s))
                cardSub.Remove(s);

        var cardKw = new HashSet<string>(card.GetKeywordsArray(), StringComparer.OrdinalIgnoreCase);

        int subtypeOverlapQty = 0;
        int keywordOverlapQty = 0;

        foreach (var entity in entities)
        {
            if (!IncludeEntityForCohesion(entity)) continue;
            if (!cardMap.TryGetValue(entity.CardId, out var other)) continue;
            if (string.Equals(other.Uuid, card.Uuid, StringComparison.OrdinalIgnoreCase)) continue;

            int qty = entity.Quantity;
            if (cardSub.Count > 0)
            {
                foreach (var ost in other.GetSubtypesArray())
                {
                    if (cardSub.Contains(ost))
                    {
                        subtypeOverlapQty += qty;
                        break;
                    }
                }
            }

            if (cardKw.Count > 0)
            {
                foreach (var okw in other.GetKeywordsArray())
                {
                    if (cardKw.Contains(okw))
                    {
                        keywordOverlapQty += qty;
                        break;
                    }
                }
            }
        }

        var parts = new List<string>();
        if (subtypeOverlapQty > 0)
            parts.Add($"{subtypeOverlapQty} deck cards share a subtype");
        if (keywordOverlapQty > 0)
            parts.Add($"{keywordOverlapQty} deck cards share a keyword");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }
}
