using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using AetherVault.Core;
using AetherVault.Models;

namespace AetherVault.Services.DeckBuilder;

/// <summary>
/// In-memory cache for deck add-cards "popular list" search results, keyed by MTG DB identity, catalog
/// revision, list id, legality format, collection-only, and optional name filter. Re-opening the same list reuses work.
/// </summary>
public sealed class DeckBrowseListResultCache
{
    private static string GetMtgDatabaseIdentity()
    {
        var v = AppDataManager.GetLocalDatabaseVersion();
        if (!string.IsNullOrEmpty(v)) return v;

        var path = AppDataManager.GetMtgDatabasePath();
        try
        {
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                return $"f:{fi.Length}|t:{fi.LastWriteTimeUtc.Ticks}";
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return "unknown";
    }

    private static string? NormalizeNamePart(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    private readonly ConcurrentDictionary<CacheKey, Card[]> _cache = new();

    private static CacheKey BuildKey(
        string listKey,
        DeckFormat format,
        bool collectionOnly,
        string? namePart) =>
        new(
            GetMtgDatabaseIdentity(),
            DeckBrowseListCatalog.CacheRevision,
            listKey,
            (int)format,
            collectionOnly,
            NormalizeNamePart(namePart));

    public bool TryGet(
        string listKey,
        DeckFormat format,
        bool collectionOnly,
        string? namePart,
        [NotNullWhen(true)] out Card[]? cards)
    {
        if (_cache.TryGetValue(BuildKey(listKey, format, collectionOnly, namePart), out var arr))
        {
            cards = [.. arr];
            return true;
        }

        cards = null;
        return false;
    }

    public void Set(
        string listKey,
        DeckFormat format,
        bool collectionOnly,
        string? namePart,
        Card[] cards) =>
        _cache[BuildKey(listKey, format, collectionOnly, namePart)] = [.. cards];

    private readonly record struct CacheKey(
        string DatabaseIdentity,
        int CatalogCacheRevision,
        string ListKey,
        int DeckFormatValue,
        bool CollectionOnly,
        string? NamePart);
}
