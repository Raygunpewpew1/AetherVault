using System.Text.Json;

namespace AetherVault.Services;

/// <summary>
/// Persists the last few plain name-only search strings for the Search tab (MRU, max 5).
/// </summary>
public static class SearchRecentQueriesStore
{
    private const string Key = "search_recent_queries_v1";
    private const int MaxCount = 5;
    private const int MaxQueryLength = 160;

    public static IReadOnlyList<string> Load()
    {
        var raw = Preferences.Default.Get(Key, "");
        if (string.IsNullOrEmpty(raw))
            return [];

        try
        {
            var arr = JsonSerializer.Deserialize<string[]>(raw);
            return arr ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Adds <paramref name="query"/> to the front (case-insensitive dedupe), capped at <see cref="MaxCount"/>.</summary>
    public static void Push(string query)
    {
        query = query.Trim();
        if (query.Length == 0)
            return;
        if (query.Length > MaxQueryLength)
            query = query[..MaxQueryLength];

        var list = new List<string>(Load());
        list.RemoveAll(x => string.Equals(x, query, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, query);
        while (list.Count > MaxCount)
            list.RemoveAt(list.Count - 1);

        Preferences.Default.Set(Key, JsonSerializer.Serialize(list));
    }
}
