using AetherVault.Core;
using AetherVault.Data;
using AetherVault.Models;
using AetherVault.Services.DeckBuilder;

namespace AetherVault.Services;

/// <summary>
/// Result of importing a single MTGJSON deck into the app.
/// </summary>
public sealed class MTGJsonDeckImportResult
{
    public int DeckId { get; set; }
    public int CardsAdded { get; set; }
    public List<string> MissingUuids { get; set; } = [];
    public bool Success => DeckId > 0;
}

/// <summary>
/// Imports an MTGJSON deck (mainBoard, sideBoard, commander) into the app by resolving UUIDs
/// against the local card DB and creating a new deck via DeckBuilderService.
/// </summary>
public class MTGJsonDeckImporter
{
    private readonly DeckBuilderService _deckService;
    private readonly ICardRepository _cardRepo;

    public MTGJsonDeckImporter(DeckBuilderService deckService, ICardRepository cardRepo)
    {
        _deckService = deckService;
        _cardRepo = cardRepo;
    }

    /// <summary>
    /// Imports the given MTGJSON deck as a new deck. Returns the new deck id and counts.
    /// </summary>
    public async Task<MTGJsonDeckImportResult> ImportDeckAsync(MtgJsonDeck deck, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new MTGJsonDeckImportResult();
        if (deck == null)
            return result;

        var format = EnumExtensions.ParseDeckFormat(deck.Type);
        progress?.Report("Creating deck...");
        var deckId = await _deckService.CreateDeckAsync(deck.Name, format, deck.ReleaseDate ?? "");
        result.DeckId = deckId;
        if (deckId <= 0)
            return result;

        var allCards = new List<(string Section, MtgJsonDeckCard Card)>();
        foreach (var c in deck.MainBoard)
            allCards.Add(("Main", c));
        foreach (var c in deck.SideBoard)
            allCards.Add(("Sideboard", c));
        if (deck.Commander != null)
        {
            foreach (var c in deck.Commander)
                allCards.Add(("Commander", c));
        }

        var uuids = allCards.Select(x => x.Card.Uuid).Distinct().ToArray();
        progress?.Report($"Resolving {uuids.Length} cards...");
        var cardMap = await _cardRepo.GetCardsByUUIDsAsync(uuids);
        var missing = uuids.Where(u => !cardMap.ContainsKey(u)).ToList();
        result.MissingUuids = missing;

        var commanderCards = deck.Commander ?? [];
        var firstCommanderUuid = commanderCards.Count > 0 ? commanderCards[0].Uuid : null;
        if (!string.IsNullOrEmpty(firstCommanderUuid) && cardMap.ContainsKey(firstCommanderUuid))
        {
            progress?.Report("Setting commander...");
            await _deckService.SetCommanderAsync(deckId, firstCommanderUuid);
            result.CardsAdded += 1;
        }

        foreach (var (section, mtgCard) in allCards)
        {
            ct.ThrowIfCancellationRequested();
            if (!cardMap.ContainsKey(mtgCard.Uuid))
                continue;
            if (section == "Commander" && mtgCard.Uuid == firstCommanderUuid)
                continue; // already added by SetCommanderAsync

            var addResult = await _deckService.AddCardAsync(deckId, mtgCard.Uuid, mtgCard.Count, section);
            if (!addResult.IsError)
                result.CardsAdded += mtgCard.Count;
        }

        return result;
    }
}
