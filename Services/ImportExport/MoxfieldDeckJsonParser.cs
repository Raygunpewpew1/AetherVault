using System.Text.Json;

namespace AetherVault.Services.ImportExport;

/// <summary>
/// Maps Moxfield <c>GET api2.moxfield.com/v3/decks/all/{id}</c> JSON into import rows.
/// </summary>
internal static class MoxfieldDeckJsonParser
{
    internal static bool TryBuildRows(string json, out string? deckName, out string? formatText, out List<DeckCsvRowV1> rows, out string? error)
    {
        deckName = null;
        formatText = null;
        rows = [];
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            deckName = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            formatText = root.TryGetProperty("format", out var fmtEl) ? fmtEl.GetString() : null;

            if (!root.TryGetProperty("boards", out var boards))
            {
                error = "Moxfield response missing boards.";
                return false;
            }

            // Order: commanders first so DeckImporter sets commander before main/side.
            TryAddBoard(boards, "commanders", DeckCsvV1.Sections.Commander, rows);
            TryAddBoard(boards, "mainboard", DeckCsvV1.Sections.Main, rows);
            TryAddBoard(boards, "sideboard", DeckCsvV1.Sections.Sideboard, rows);
            TryAddBoard(boards, "companions", DeckCsvV1.Sections.Sideboard, rows);
            TryAddBoard(boards, "signatureSpells", DeckCsvV1.Sections.Sideboard, rows);

            if (rows.Count == 0)
            {
                error = "Moxfield deck has no mainboard or commander cards.";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Could not read Moxfield data: {ex.Message}";
            return false;
        }
    }

    private static void TryAddBoard(JsonElement boards, string boardKey, string section, List<DeckCsvRowV1> rows)
    {
        if (!boards.TryGetProperty(boardKey, out var board))
            return;
        if (!board.TryGetProperty("cards", out var cards))
            return;
        AddCardsFromBoard(cards, section, rows);
    }

    private static void AddCardsFromBoard(JsonElement cardsContainer, string section, List<DeckCsvRowV1> rows)
    {
        if (cardsContainer.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in cardsContainer.EnumerateObject())
        {
            var slot = prop.Value;
            if (slot.ValueKind != JsonValueKind.Object)
                continue;

            if (!slot.TryGetProperty("quantity", out var qtyEl) || qtyEl.ValueKind != JsonValueKind.Number)
                continue;
            int qty = qtyEl.GetInt32();
            if (qty <= 0)
                continue;

            if (!slot.TryGetProperty("card", out var card) || card.ValueKind != JsonValueKind.Object)
                continue;

            string? cardName = card.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(cardName))
                continue;

            string? setCode = card.TryGetProperty("set", out var s) ? s.GetString() : null;
            string? cn = card.TryGetProperty("cn", out var cnEl) ? cnEl.GetString() : null;
            string? scryfallId = card.TryGetProperty("scryfall_id", out var sf) ? sf.GetString() : null;

            rows.Add(new DeckCsvRowV1
            {
                DeckName = "",
                Format = null,
                Section = section,
                Quantity = qty,
                CardUuid = null,
                CardName = cardName.Trim(),
                SetCode = string.IsNullOrWhiteSpace(setCode) ? null : setCode.Trim(),
                CollectorNumber = string.IsNullOrWhiteSpace(cn) ? null : cn.Trim(),
                ScryfallId = string.IsNullOrWhiteSpace(scryfallId) ? null : scryfallId.Trim(),
            });
        }
    }
}
