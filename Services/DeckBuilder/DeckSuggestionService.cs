using AetherVault.Core;
using AetherVault.Data;
using AetherVault.Models;

namespace AetherVault.Services.DeckBuilder;

/// <summary>
/// Commander-style deck card suggestions: legal candidates, color identity, role gaps, cohesion, and archetype heuristics.
/// </summary>
public static class DeckSuggestionService
{
    private const int CandidateSqlLimit = 450;
    private const int DefaultResultLimit = 40;

    /// <summary>Weighted score for tests and tuning.</summary>
    public static int ComputeSuggestionScore(
        Card card,
        CommanderArchetype archetype,
        Card? commander,
        DeckStats deckStats,
        DeckCohesionProfile cohesion)
    {
        if (card.IsBasicLand) return int.MinValue / 4;

        string textLower = (card.Text ?? "").ToLowerInvariant();
        int score = EdhRecPopularityScore(card);
        score += CohesionSubtypeScore(card, cohesion);
        score += CommanderSubtypeOverlap(commander, card);
        score += RoleGapScore(card, deckStats);
        score += ArchetypeScore(card, archetype, textLower, deckStats);
        return score;
    }

    public static async Task<Card[]> GetSuggestionsAsync(
        ICardRepository cardRepository,
        DeckEntity deck,
        CommanderArchetype archetype,
        Card? commanderCard,
        IReadOnlyList<DeckCardEntity> deckEntities,
        IReadOnlyDictionary<string, Card> cardMap,
        DeckStats deckStats,
        DeckCohesionProfile cohesionProfile,
        bool collectionOnly,
        int maxResults = DefaultResultLimit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var format = EnumExtensions.ParseDeckFormat(deck.Format);
        if (!format.IsCommanderLikeRules())
            return [];

        ColorIdentity commanderIdentity = ColorIdentity.Empty;
        if (!string.IsNullOrWhiteSpace(deck.CommanderId))
        {
            var (resolved, had, warn) = await DeckColorIdentityResolver.TryResolveCommanderDeckColorIdentityAsync(
                    cardRepository, deck, deckEntities, cardMap)
                .ConfigureAwait(false);
            if (had && warn == null)
                commanderIdentity = resolved;
            else if (commanderCard != null)
                commanderIdentity = commanderCard.GetColorIdentity();
        }
        else if (commanderCard != null)
        {
            commanderIdentity = commanderCard.GetColorIdentity();
        }
        else if (!string.IsNullOrWhiteSpace(deck.ColorIdentity))
        {
            commanderIdentity = deck.ParsedColorIdentity;
        }

        var helper = cardRepository.CreateSearchHelper();
        helper.SearchCards();
        helper.WhereLegalIn(format);
        helper.WherePrimarySideOnly();
        if (collectionOnly)
            helper.WhereInCollection();

        helper.OrderBy("(CASE WHEN IFNULL(c.edhrecRank,0) <= 0 THEN 999999 ELSE c.edhrecRank END)");
        helper.Limit(CandidateSqlLimit);

        var candidates = await cardRepository.SearchAdvancedAsync(helper);
        cancellationToken.ThrowIfCancellationRequested();

        int maxCopies = DeckFormatRules.MaxNonBasicCopies(format);

        var scored = new List<(Card Card, int Score)>();
        foreach (var card in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!commanderIdentity.Contains(card.GetColorIdentity())) continue;
            if (IsCommanderOrEssentialDuplicate(card, deckEntities, cardMap)) continue;
            if (!CanAddCopy(card, deckEntities, maxCopies)) continue;

            int s = ComputeSuggestionScore(card, archetype, commanderCard, deckStats, cohesionProfile);
            if (s <= int.MinValue / 8) continue;
            scored.Add((card, s));
        }

        scored.Sort((a, b) =>
        {
            int c = b.Score.CompareTo(a.Score);
            return c != 0 ? c : string.Compare(a.Card.Name, b.Card.Name, StringComparison.OrdinalIgnoreCase);
        });

        var picked = new List<Card>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (card, _) in scored)
        {
            if (!names.Add(card.Name)) continue;
            picked.Add(card);
            if (picked.Count >= maxResults) break;
        }

        return [.. picked];
    }

    private static bool IsCommanderOrEssentialDuplicate(Card card, IReadOnlyList<DeckCardEntity> deckEntities, IReadOnlyDictionary<string, Card> cardMap)
    {
        foreach (var e in deckEntities)
        {
            if (e.Quantity <= 0) continue;
            if (string.Equals(e.Section, "Commander", StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.CardId, card.Uuid, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!cardMap.TryGetValue(e.CardId, out var inc)) continue;
            if (string.Equals(inc.Name, card.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(e.Section, "Commander", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static bool CanAddCopy(Card card, IReadOnlyList<DeckCardEntity> deckEntities, int maxCopies)
    {
        if (card.IsBasicLand) return true;
        if ((card.Text ?? "").Contains("A deck can have any number of cards named", StringComparison.OrdinalIgnoreCase))
            return true;

        int total = 0;
        foreach (var e in deckEntities)
        {
            if (e.Quantity <= 0) continue;
            if (string.Equals(e.CardId, card.Uuid, StringComparison.OrdinalIgnoreCase))
                total += e.Quantity;
        }

        return total < maxCopies;
    }

    private static int EdhRecPopularityScore(Card card)
    {
        int r = card.EdhRecRank;
        if (r <= 0) return 0;
        if (r <= 300) return 55;
        if (r <= 1200) return 35;
        if (r <= 4000) return 18;
        return 6;
    }

    private static int CohesionSubtypeScore(Card card, DeckCohesionProfile cohesion)
    {
        if (cohesion.SubtypeTotals.Count == 0) return 0;
        var sub = card.GetSubtypesArray();
        if (sub.Length == 0) return 0;

        int add = 0;
        foreach (var (label, weight) in TopEntries(cohesion.SubtypeTotals, 6))
        {
            foreach (var st in sub)
            {
                if (string.Equals(st, label, StringComparison.OrdinalIgnoreCase))
                {
                    add += 10 + Math.Min(weight, 8);
                    break;
                }
            }
        }

        return add;
    }

    private static IEnumerable<(string Key, int Value)> TopEntries(Dictionary<string, int> map, int max)
    {
        return map
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(kv => (kv.Key, kv.Value));
    }

    private static int CommanderSubtypeOverlap(Card? commander, Card card)
    {
        if (commander == null) return 0;
        var a = commander.GetSubtypesArray();
        if (a.Length == 0) return 0;
        var set = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
        int n = 0;
        foreach (var st in card.GetSubtypesArray())
        {
            if (set.Contains(st)) n++;
        }

        return n * 14;
    }

    private static int RoleGapScore(Card card, DeckStats s)
    {
        int score = 0;
        if (s.RemovalCount < 10 && DeckCardRoleClassifier.CardMatchesRemovalHeuristic(card)) score += 22;
        if (s.CardDrawCount < 10 && DeckCardRoleClassifier.CardMatchesCardDrawHeuristic(card)) score += 18;
        if (s.RampCount < 12 && DeckCardRoleClassifier.CardMatchesRampHeuristic(card)) score += 16;
        if (s.CounterspellsCount < 6 && DeckCardRoleClassifier.CardMatchesCounterspellHeuristic(card)) score += 20;
        if (s.BoardWipesCount < 3 && DeckCardRoleClassifier.CardMatchesBoardWipeHeuristic(card)) score += 14;
        return score;
    }

    private static int ArchetypeScore(Card card, CommanderArchetype archetype, string textLower, DeckStats deckStats)
    {
        bool creature = card.IsCreature;
        double cmc = card.EffectiveManaValue;

        return archetype switch
        {
            CommanderArchetype.Aggro => creature && cmc <= 3 ? 18 :
                textLower.Contains("haste", StringComparison.Ordinal) ? 14 :
                card.IsInstant && cmc <= 2 ? 10 : 0,

            CommanderArchetype.Midrange => creature && cmc is >= 3 and <= 6 ? 14 :
                (card.IsSorcery || card.IsInstant) && cmc is >= 2 and <= 5 ? 10 : 0,

            CommanderArchetype.Control => (deckStats.CounterspellsCount < 8 && DeckCardRoleClassifier.CardMatchesCounterspellHeuristic(card)) ? 12 :
                (deckStats.BoardWipesCount < 4 && DeckCardRoleClassifier.CardMatchesBoardWipeHeuristic(card)) ? 12 :
                (card.IsPlaneswalker && cmc >= 4) ? 10 : 0,

            CommanderArchetype.Combo => textLower.Contains("search your library", StringComparison.Ordinal) ? 25 :
                textLower.Contains("untap all", StringComparison.Ordinal) || textLower.Contains("untap target", StringComparison.Ordinal) ? 12 : 0,

            CommanderArchetype.Tribal => 0,

            CommanderArchetype.Voltron => textLower.Contains("equip", StringComparison.Ordinal) ? 18 :
                textLower.Contains("enchant creature", StringComparison.Ordinal) ? 14 :
                textLower.Contains("aura", StringComparison.Ordinal) && card.IsEnchantment ? 10 : 0,

            CommanderArchetype.Spellslinger => !card.IsLand && (card.IsInstant || card.IsSorcery) && cmc <= 6 ? 16 : 0,

            CommanderArchetype.Tokens => textLower.Contains("create", StringComparison.Ordinal) && textLower.Contains("token", StringComparison.Ordinal) ? 22 : 0,

            CommanderArchetype.Graveyard => textLower.Contains("graveyard", StringComparison.Ordinal) ? 16 :
                textLower.Contains("mill", StringComparison.Ordinal) ? 10 : 0,

            CommanderArchetype.Landfall => textLower.Contains("landfall", StringComparison.Ordinal) ? 26 : 0,

            CommanderArchetype.Aristocrats => textLower.Contains("sacrifice", StringComparison.Ordinal) || textLower.Contains("sacrifice a", StringComparison.Ordinal) ? 18 :
                textLower.Contains("dies", StringComparison.Ordinal) ? 8 : 0,

            CommanderArchetype.Stax => textLower.Contains("each opponent", StringComparison.Ordinal) && textLower.Contains("pay", StringComparison.Ordinal) ? 14 :
                textLower.Contains("can't", StringComparison.Ordinal) ? 10 : 0,

            CommanderArchetype.GroupHug => textLower.Contains("each player draws", StringComparison.Ordinal) ? 12 :
                textLower.Contains("each player may", StringComparison.Ordinal) ? 8 : 0,

            CommanderArchetype.SuperFriends => card.IsPlaneswalker ? 22 : 0,

            _ => creature && cmc <= 4 ? 6 : 0
        };
    }

}
