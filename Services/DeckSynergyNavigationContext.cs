using AetherVault.Models;
using AetherVault.Services.DeckBuilder;

namespace AetherVault.Services;

/// <summary>
/// When opening card detail from a deck, holds cohesion data so the detail page can show overlap hints.
/// Cleared when opening card detail from search or collection.
/// </summary>
public sealed class DeckSynergyNavigationContext
{
    private List<DeckCardEntity>? _entities;
    private Dictionary<string, Card>? _cardMap;

    public int? SourceDeckId { get; private set; }

    public void SetDeckContext(int deckId, IReadOnlyList<DeckCardEntity> entities, IReadOnlyDictionary<string, Card> cardMap)
    {
        SourceDeckId = deckId;
        _entities = [.. entities];
        _cardMap = new Dictionary<string, Card>(cardMap, StringComparer.OrdinalIgnoreCase);
    }

    public void Clear()
    {
        SourceDeckId = null;
        _entities = null;
        _cardMap = null;
    }

    public string? GetOverlapHint(Card card)
    {
        if (_entities == null || _cardMap == null || string.IsNullOrEmpty(card.Uuid))
            return null;
        return DeckCohesionAnalyzer.FormatOverlapHint(card, _entities, _cardMap);
    }
}
