using AetherVault.Core;
using AetherVault.Models;
using Dapper;
using System.Data.Common;

namespace AetherVault.Data;

/// <summary>
/// Read-only access to the MTG card database (cards + tokens). All queries go through the MTG connection.
/// Uses Dapper for execution and CardMapper to turn rows into Card objects. Never write to this DB from here.
/// </summary>
public class CardRepository : ICardRepository
{
    private readonly DatabaseManager _db;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CardRepository(DatabaseManager databaseManager)
    {
        _db = databaseManager;
    }

    public Task<Card> GetCardByUuidAsync(string uuid) => GetCardWithLegalitiesAsync(uuid);

    /// <summary>Loads a single card by UUID from cards/tokens and maps it to a Card model.</summary>
    public async Task<Card> GetCardWithLegalitiesAsync(string uuid)
    {
        if (_db.IsAtomicCatalog)
            return await LoadAtomicCardAsync(uuid, includeRulings: false);

        return await WithMtgReaderAsync(
            SqlQueries.BaseCardsAndTokens + SqlQueries.WhereUuidEquals,
            new { uuid },
            async reader =>
            {
                var o = new CardMapper.CardOrdinals(reader);
                return await reader.ReadAsync() ? CardMapper.MapCard(reader, o) : new Card();
            });
    }

    public async Task<Card> GetCardDetailsAsync(string uuid)
    {
        var card = await GetCardWithLegalitiesAsync(uuid);

        if (card.Layout is CardLayout.Adventure or CardLayout.Split)
        {
            var otherFaces = await GetCardWithOtherFacesAsync(uuid);
            if (otherFaces.Length > 1)
                card.Text = otherFaces[0].Text + Environment.NewLine + "---" + Environment.NewLine + otherFaces[1].Text;
        }

        return card;
    }

    public async Task<Card> GetCardWithRulingsAsync(string uuid)
    {
        if (_db.IsAtomicCatalog)
            return await LoadAtomicCardAsync(uuid, includeRulings: true);

        var card = await GetCardWithLegalitiesAsync(uuid);
        if (!string.IsNullOrEmpty(card.Uuid))
            card.Rulings = (await GetCardRulingsAsync(uuid)).ToList();
        return card;
    }

    public async Task<Card> GetCardByFaceNameAndSetAsync(string faceName, string setCode)
    {
        return await WithMtgReaderAsync(
            SqlQueries.SelectFullCard + " WHERE c.faceName = @fname AND c.setCode = @set LIMIT 1",
            new { fname = faceName, set = setCode },
            async reader =>
            {
                var o = new CardMapper.CardOrdinals(reader);
                return await reader.ReadAsync() ? CardMapper.MapCard(reader, o) : new Card();
            });
    }

    public async Task<Card?> GetCardByNameAndSetAsync(string name, string setCode)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(setCode))
            return null;
        var card = await WithMtgReaderAsync(
            SqlQueries.BaseCardsAndTokens + SqlQueries.WhereNameAndSet,
            new { name = name.Trim(), set = setCode.Trim() },
            async reader =>
            {
                var o = new CardMapper.CardOrdinals(reader);
                return await reader.ReadAsync() ? CardMapper.MapCard(reader, o) : null;
            });
        return card?.Uuid != null ? card : null;
    }

    public async Task<Card?> GetCardByScryfallIdAsync(string scryfallId)
    {
        if (string.IsNullOrWhiteSpace(scryfallId))
            return null;
        var card = await WithMtgReaderAsync(
            SqlQueries.BaseCardsAndTokens + SqlQueries.WhereScryfallId,
            new { sid = scryfallId.Trim() },
            async reader =>
            {
                var o = new CardMapper.CardOrdinals(reader);
                return await reader.ReadAsync() ? CardMapper.MapCard(reader, o) : null;
            });
        return card?.Uuid != null ? card : null;
    }

    public async Task<string> GetScryfallIdAsync(string cardUuid)
    {
        await _lock.WaitAsync();
        try
        {
            if (_db.IsAtomicCatalog)
            {
                var c = await LoadAtomicCardAsync(cardUuid, includeRulings: false);
                return c.ScryfallId;
            }

            var result = await _db.MtgConnection.QueryFirstOrDefaultAsync<string>(
                SqlQueries.SelectScryfallId, new { uuid = cardUuid });
            return result ?? "";
        }
        finally
        {
            _lock.Release();
        }
    }

    private class CardRulingRow
    {
        public string Date { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public async Task<CardRuling[]> GetCardRulingsAsync(string uuid)
    {
        await _lock.WaitAsync();
        try
        {
            if (_db.IsAtomicCatalog)
            {
                var match = AtomicCatalogMapper.SqlAliasMatchesIdParameter("a", "id");
                var json = await _db.MtgConnection.QueryFirstOrDefaultAsync<string>(
                    $"""
                    SELECT a.rulings_json FROM atomic_cards a
                    WHERE {match}
                    LIMIT 1
                    """,
                    new { id = uuid });
                return [.. AtomicCatalogMapper.ParseRulings(json)];
            }

            var rows = await _db.MtgConnection.QueryAsync<CardRulingRow>(
                SqlQueries.SelectRulings, new { uuid });

            var rulings = new List<CardRuling>();
            foreach (var row in rows)
            {
                DateTime.TryParse(row.Date, out var date);
                rulings.Add(new CardRuling(date, row.Text));
            }

            return [.. rulings];
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string[]> GetOtherFaceIdsAsync(string uuid)
    {
        await _lock.WaitAsync();
        try
        {
            if (_db.IsAtomicCatalog)
            {
                var match = AtomicCatalogMapper.SqlAliasMatchesIdParameter("a", "id");
                var self = await _db.MtgConnection.QueryFirstOrDefaultAsync<AtomicIdNameRow>(
                    $"""
                    SELECT a.id AS Id, a.name AS Name FROM atomic_cards a
                    WHERE {match}
                    LIMIT 1
                    """,
                    new { id = uuid });
                if (self is null || string.IsNullOrEmpty(self.Name))
                    return [];

                var siblingRows = await _db.MtgConnection.QueryAsync<AtomicCatalogMapper.AtomicRow>(
                    """
                    SELECT
                      a.id, a.name, a.face_index, a.ascii_name, a.face_name, a.mana_cost, a.mana_value,
                      a.type_line, a.oracle_text, a.power, a.toughness, a.loyalty, a.defense, a.layout,
                      a.colors, a.color_identity, a.keywords, a.scryfall_id, a.scryfall_oracle_id,
                      a.identifiers_json, a.first_printing, a.printings_json, a.legalities_json, a.rulings_json, a.related_json,
                      a.leadership_json, a.is_reserved, a.is_funny
                    FROM atomic_cards a
                    WHERE a.name = @n AND a.id <> @sid
                    ORDER BY a.face_index
                    """,
                    new { n = self.Name, sid = self.Id });
                return [.. siblingRows
                    .Select(r => AtomicCatalogMapper.ToCard(r).Uuid)
                    .Where(static s => !string.IsNullOrEmpty(s))];
            }

            var raw = await _db.MtgConnection.QueryFirstOrDefaultAsync<string>(
                SqlQueries.SelectOtherFaces, new { uuid });
            return CardMapper.ParseOtherFaceIds(raw ?? "");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Card[]> GetCardWithOtherFacesAsync(string uuid)
    {
        var otherIds = await GetOtherFaceIdsAsync(uuid);
        var allIds = new[] { uuid }.Concat(otherIds).ToArray();
        var dict = await GetCardsByUuiDsAsync(allIds);
        return SortCardsBySide([.. dict.Values]);
    }

    public async Task<Card[]> GetFullCardPackageAsync(string uuid)
    {
        if (_db.IsAtomicCatalog)
        {
            var atomicMain = await GetCardWithRulingsAsync(uuid);
            if (string.IsNullOrEmpty(atomicMain.Uuid)) return [];

            await _lock.WaitAsync();
            try
            {
                var rows = await _db.MtgConnection.QueryAsync<AtomicCatalogMapper.AtomicRow>(
                    """
                    SELECT
                      a.id, a.name, a.face_index, a.ascii_name, a.face_name, a.mana_cost, a.mana_value,
                      a.type_line, a.oracle_text, a.power, a.toughness, a.loyalty, a.defense, a.layout,
                      a.colors, a.color_identity, a.keywords, a.scryfall_id, a.scryfall_oracle_id,
                      a.identifiers_json, a.first_printing, a.printings_json, a.legalities_json, a.rulings_json, a.related_json,
                      a.leadership_json, a.is_reserved, a.is_funny
                    FROM atomic_cards a
                    WHERE a.name = @n
                    ORDER BY a.face_index
                    """,
                    new { n = atomicMain.Name });

                var list = new List<Card>();
                foreach (var r in rows)
                {
                    var c = AtomicCatalogMapper.ToCard(r);
                    if (AtomicCatalogMapper.RowMatchesLookupId(r, uuid))
                        c.Rulings = AtomicCatalogMapper.ParseRulings(r.rulings_json);
                    list.Add(c);
                }

                return [.. list];
            }
            finally
            {
                _lock.Release();
            }
        }

        var mainCard = await GetCardWithRulingsAsync(uuid);
        if (string.IsNullOrEmpty(mainCard.Uuid)) return [];

        var cards = new List<Card> { mainCard };

        // CASE A: Meld cards (linked by name in CardParts JSON)
        if (mainCard.Layout == CardLayout.Meld && !string.IsNullOrEmpty(mainCard.CardParts))
        {
            var cardParts = CardMapper.ParseJsonArrayToStrings(mainCard.CardParts);
            if (cardParts.Length > 0)
                cards.AddRange(await GetMeldPartCardsAsync(cardParts, mainCard.SetCode, mainCard.Uuid));
        }
        // CASE B: Standard multi-face (Transform, Adventure, Split, etc.)
        else
        {
            var otherIds = CardMapper.ParseOtherFaceIds(mainCard.OtherFaceIds);
            if (otherIds.Length > 0)
            {
                var dict = await GetCardsByUuiDsAsync(otherIds);
                cards.AddRange(dict.Values);
            }
        }

        // Include tokens/related cards
        if (mainCard.RelatedCards != null && mainCard.RelatedCards.Length > 0)
        {
            var dict = await GetCardsByUuiDsAsync(mainCard.RelatedCards);
            foreach (var kvp in dict)
            {
                // Ensure we don't duplicate (e.g. if a related card was already included)
                if (!cards.Any(c => c.Uuid == kvp.Key))
                {
                    cards.Add(kvp.Value);
                }
            }
        }

        return SortCardsBySide([.. cards]);
    }

    public async Task<Dictionary<string, Card>> GetCardsByUuiDsAsync(string[] uuids)
    {
        var result = new Dictionary<string, Card>(StringComparer.OrdinalIgnoreCase);
        if (uuids.Length == 0) return result;

        if (_db.IsAtomicCatalog)
        {
            const int chunkSize = 100;
            for (int i = 0; i < uuids.Length; i += chunkSize)
            {
                var chunk = uuids.Skip(i).Take(chunkSize).ToArray();
                var parts = new List<string>(chunk.Length);
                var dynamicParams = new DynamicParameters();
                for (int j = 0; j < chunk.Length; j++)
                {
                    var p = "u" + (i + j);
                    parts.Add(AtomicCatalogMapper.SqlAliasMatchesIdParameter("a", p));
                    dynamicParams.Add(p, chunk[j]);
                }

                var sql =
                    $"""
                    SELECT
                      a.id, a.name, a.face_index, a.ascii_name, a.face_name, a.mana_cost, a.mana_value,
                      a.type_line, a.oracle_text, a.power, a.toughness, a.loyalty, a.defense, a.layout,
                      a.colors, a.color_identity, a.keywords, a.scryfall_id, a.scryfall_oracle_id,
                      a.identifiers_json, a.first_printing, a.printings_json, a.legalities_json, a.rulings_json, a.related_json,
                      a.leadership_json, a.is_reserved, a.is_funny
                    FROM atomic_cards a
                    WHERE {string.Join(" OR ", parts)}
                    """;

                await _lock.WaitAsync();
                try
                {
                    var rows = await _db.MtgConnection.QueryAsync<AtomicCatalogMapper.AtomicRow>(sql, dynamicParams);
                    foreach (var r in rows)
                    {
                        var card = AtomicCatalogMapper.ToCard(r);
                        AtomicCatalogMapper.AddAtomicRowToLookupDictionary(result, r, card);
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }

            return result;
        }

        const int fullChunk = 500;
        for (int i = 0; i < uuids.Length; i += fullChunk)
        {
            var chunk = uuids.Skip(i).Take(fullChunk).ToArray();
            var paramNames = chunk.Select((_, idx) => $"@u{idx}").ToArray();
            var sql = SqlQueries.BaseCardsAndTokens + " WHERE c.uuid IN (" + string.Join(",", paramNames) + ")";

            var dynamicParams = new DynamicParameters();
            for (int j = 0; j < chunk.Length; j++)
            {
                dynamicParams.Add(paramNames[j], chunk[j]);
            }

            await WithMtgReaderAsync(sql,
                dynamicParams,
                async reader =>
                {
                    var o = new CardMapper.CardOrdinals(reader);
                    while (await reader.ReadAsync())
                    {
                        var card = CardMapper.MapCard(reader, o);
                        result[card.Uuid] = card;
                    }
                    return 0;
                });
        }

        return result;
    }

    public async Task<IReadOnlyList<ImportLookupRow>> GetImportLookupRowsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_db.IsAtomicCatalog)
                return [];

            var rows = await _db.MtgConnection.QueryAsync<ImportLookupRow>(SqlQueries.SelectImportLookupRows);
            return [.. rows];
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Card[]> SearchCardsAsync(string searchText, int limit = 100)
    {
        if (_db.IsAtomicCatalog)
        {
            var options = new SearchOptions
            {
                NameFilter = searchText,
                PrimarySideOnly = true,
                IncludeTokens = false
            };
            return await SearchAtomicCatalogAsync(options, limit, 0);
        }

        var helper = CreateSearchHelper();
        helper.SearchCards()
            .WhereNameContains(searchText)
            .WherePrimarySideOnly()
            .OrderBy("c.name")
            .Limit(limit);
        return await SearchCardsAdvancedAsync(helper);
    }

    public async Task<Card[]> SearchCardsAdvancedAsync(MtgSearchHelper searchHelper)
    {
        if (_db.IsAtomicCatalog)
        {
            if (searchHelper.IsCollectionQuery || searchHelper.IsSetsQuery || searchHelper.IsCardTableQuery)
                return [];
        }

        var cards = new List<Card>();
        var (sql, parameters) = searchHelper.Build();

        var dynamicParams = new DynamicParameters();
        foreach (var (name, value) in parameters)
        {
            dynamicParams.Add(name, value);
        }

        await WithMtgReaderAsync(sql,
            dynamicParams,
            async reader =>
            {
                var o = new CardMapper.CardOrdinals(reader);
                while (await reader.ReadAsync())
                    cards.Add(CardMapper.MapCard(reader, o));
                return 0;
            });

        return [.. cards];
    }

    public async Task<int> GetCountAdvancedAsync(MtgSearchHelper searchHelper)
    {
        if (_db.IsAtomicCatalog)
            return 0;

        var (sql, parameters) = searchHelper.BuildCount();

        await _lock.WaitAsync();
        try
        {
            var dynamicParams = new DynamicParameters();
            foreach (var (name, value) in parameters)
            {
                dynamicParams.Add(name, value);
            }

            return await _db.MtgConnection.ExecuteScalarAsync<int>(sql, dynamicParams);
        }
        finally
        {
            _lock.Release();
        }
    }

    public MtgSearchHelper CreateSearchHelper() => new();

    public async Task<IReadOnlyList<SetInfo>> GetAllSetsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_db.IsAtomicCatalog)
                return [];

            var list = await _db.MtgConnection.QueryAsync<SetInfo>(SqlQueries.SelectSetsForFilter);
            return [.. list];
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> HasFtsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_db.IsAtomicCatalog)
            {
                var val = await _db.MtgConnection.QueryFirstOrDefaultAsync<int>(SqlQueries.AtomicFtsExistsCheck);
                return val == 1;
            }

            var valFull = await _db.MtgConnection.QueryFirstOrDefaultAsync<int>(SqlQueries.FtsExistsCheck);
            return valFull == 1;
        }
        catch
        {
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Card[]> SearchAtomicCatalogAsync(SearchOptions options, int limit, int offset)
    {
        if (!_db.IsAtomicCatalog)
            return [];

        var (sql, parameters) = AtomicCatalogQuery.BuildSearch(options, limit, offset);
        await _lock.WaitAsync();
        try
        {
            var rows = await _db.MtgConnection.QueryAsync<AtomicCatalogMapper.AtomicRow>(sql, parameters);
            return [.. rows.Select(AtomicCatalogMapper.ToCard)];
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> CountAtomicCatalogAsync(SearchOptions options)
    {
        if (!_db.IsAtomicCatalog)
            return 0;

        var (sql, parameters) = AtomicCatalogQuery.BuildCount(options);
        await _lock.WaitAsync();
        try
        {
            return await _db.MtgConnection.ExecuteScalarAsync<int>(sql, parameters);
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Private helpers ─────────────────────────────────────────────

    private sealed class AtomicIdNameRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private async Task<Card> LoadAtomicCardAsync(string id, bool includeRulings)
    {
        await _lock.WaitAsync();
        try
        {
            var match = AtomicCatalogMapper.SqlAliasMatchesIdParameter("a", "id");
            var row = await _db.MtgConnection.QueryFirstOrDefaultAsync<AtomicCatalogMapper.AtomicRow>(
                $"""
                SELECT
                  a.id, a.name, a.face_index, a.ascii_name, a.face_name, a.mana_cost, a.mana_value,
                  a.type_line, a.oracle_text, a.power, a.toughness, a.loyalty, a.defense, a.layout,
                  a.colors, a.color_identity, a.keywords, a.scryfall_id, a.scryfall_oracle_id,
                  a.identifiers_json, a.first_printing, a.printings_json, a.legalities_json, a.rulings_json, a.related_json,
                  a.leadership_json, a.is_reserved, a.is_funny
                FROM atomic_cards a
                WHERE {match}
                LIMIT 1
                """,
                new { id });

            if (row is null)
                return new Card();

            var card = AtomicCatalogMapper.ToCard(row);
            if (includeRulings)
                card.Rulings = AtomicCatalogMapper.ParseRulings(row.rulings_json);
            return card;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Card[]> GetMeldPartCardsAsync(string[] cardParts, string setCode, string mainUuid)
    {
        var cards = new List<Card>();
        var dynamicParams = new DynamicParameters();
        dynamicParams.Add("@set", setCode);
        dynamicParams.Add("@mainUUID", mainUuid);

        var conditions = new List<string>();
        for (int i = 0; i < cardParts.Length; i++)
        {
            dynamicParams.Add($"@n{i}", cardParts[i].Trim());
            conditions.Add($"(c.faceName = @n{i} OR c.name = @n{i})");
        }

        var sql = SqlQueries.SelectFullCard +
            $" WHERE c.setCode = @set AND ({string.Join(" OR ", conditions)}) AND c.uuid <> @mainUUID";

        await WithMtgReaderAsync(sql,
            dynamicParams,
            async reader =>
            {
                var o = new CardMapper.CardOrdinals(reader);
                while (await reader.ReadAsync())
                    cards.Add(CardMapper.MapCard(reader, o));
                return 0;
            });

        return [.. cards];
    }

    private static Card[] SortCardsBySide(Card[] cards) =>
        [.. cards.OrderBy(c => c.Side)];

    /// <summary>
    /// Executes a query against the MTG database and processes results with an async reader.
    /// Uses SemaphoreSlim for non-blocking thread safety.
    /// Uses Dapper ExecuteReaderAsync underneath.
    /// </summary>
    private async Task<T> WithMtgReaderAsync<T>(
        string sql,
        object? param,
        Func<DbDataReader, Task<T>> readFunc)
    {
        await _lock.WaitAsync();
        try
        {
            using var reader = await _db.MtgConnection.ExecuteReaderAsync(sql, param) as DbDataReader
                ?? throw new InvalidOperationException("Failed to create DbDataReader.");
            return await readFunc(reader);
        }
        finally
        {
            _lock.Release();
        }
    }
}