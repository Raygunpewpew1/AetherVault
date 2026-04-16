using AetherVault.Core;
using AetherVault.Data;
using AetherVault.Models;

namespace AetherVault.Services.DeckBuilder;

/// <summary>
/// Resolves the combined color identity for a commander-style deck from
/// <see cref="DeckEntity.CommanderId"/>, <see cref="DeckEntity.PartnerId"/>, and Commander-section rows.
/// </summary>
public static class DeckColorIdentityResolver
{
    /// <summary>
    /// Attempts to build the deck's allowed color identity union.
    /// </summary>
    /// <returns>
    /// Tuple <c>(Identity, hadSources, warning)</c>:
    /// <c>hadSources</c> is <see langword="false"/> when there is nothing to check (no commander sources).
    /// When <c>hadSources</c> is <see langword="true"/> and <c>warning</c> is non-null, callers should treat color checks as skipped (soft warning).
    /// </returns>
    public static async Task<(ColorIdentity Identity, bool hadSources, ValidationResult? warning)> TryResolveCommanderDeckColorIdentityAsync(
        ICardRepository cardRepository,
        DeckEntity deck,
        IReadOnlyList<DeckCardEntity> currentCards,
        IReadOnlyDictionary<string, Card>? cardsByUuid = null)
    {
        var orderedIds = new List<string>();
        void appendUnique(string? uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return;
            var t = uuid.Trim();
            if (orderedIds.Exists(x => x.Equals(t, StringComparison.OrdinalIgnoreCase)))
                return;
            orderedIds.Add(t);
        }

        appendUnique(deck.CommanderId);
        appendUnique(deck.PartnerId);
        foreach (var row in currentCards)
        {
            if (row.Quantity <= 0) continue;
            if (!string.Equals(row.Section, DeckCardSections.Commander, StringComparison.OrdinalIgnoreCase))
                continue;
            appendUnique(row.CardId);
        }

        if (orderedIds.Count == 0)
            return (ColorIdentity.Empty, hadSources: false, warning: null);

        var fetchCache = new Dictionary<string, Card?>(StringComparer.OrdinalIgnoreCase);
        async Task<Card?> GetOnceAsync(string uuid)
        {
            if (fetchCache.TryGetValue(uuid, out var cached))
                return cached;
            var c = await TryGetCardAsync(uuid, cardRepository, cardsByUuid).ConfigureAwait(false);
            fetchCache[uuid] = c;
            return c;
        }

        bool primaryRequired = !string.IsNullOrWhiteSpace(deck.CommanderId);
        if (primaryRequired)
        {
            var primary = await GetOnceAsync(deck.CommanderId!.Trim()).ConfigureAwait(false);
            if (primary == null || string.IsNullOrEmpty(primary.Uuid))
            {
                return (
                    ColorIdentity.Empty,
                    hadSources: true,
                    warning: ValidationResult.Warning("Commander not found, skipping color identity check."));
            }
        }

        var identities = new List<ColorIdentity>();
        foreach (var id in orderedIds)
        {
            var c = await GetOnceAsync(id).ConfigureAwait(false);
            if (c != null && !string.IsNullOrEmpty(c.Uuid))
                identities.Add(c.GetColorIdentity());
        }

        if (identities.Count == 0)
        {
            return (
                ColorIdentity.Empty,
                hadSources: true,
                warning: ValidationResult.Warning("Could not resolve commander cards for color identity check."));
        }

        return (ColorIdentity.UnionAll(identities), hadSources: true, warning: null);
    }

    private static async Task<Card?> TryGetCardAsync(
        string? uuid,
        ICardRepository cardRepository,
        IReadOnlyDictionary<string, Card>? cardsByUuid)
    {
        if (string.IsNullOrWhiteSpace(uuid)) return null;
        if (cardsByUuid != null && cardsByUuid.TryGetValue(uuid, out var fromBulk))
            return fromBulk;
        return await cardRepository.GetCardDetailsAsync(uuid).ConfigureAwait(false);
    }
}
