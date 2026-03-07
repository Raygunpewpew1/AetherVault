using System.IO.Compression;
using System.Text.Json;
using AetherVault.Models;

namespace AetherVault.Services;

/// <summary>
/// Fetches and caches the MTGJSON deck list catalog (DeckList.json) and individual deck JSON files.
/// </summary>
public class MTGJsonDeckListService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _cachePath;
    private List<MtgJsonDeckListEntry>? _cachedList;

    public MTGJsonDeckListService()
    {
        _cachePath = Path.Combine(AppDataManager.GetAppDataPath(), MTGConstants.MTGJsonDeckListCacheFile);
    }

    /// <summary>
    /// Gets the deck list catalog. Uses cached file if present and not forcing refresh.
    /// </summary>
    public async Task<IReadOnlyList<MtgJsonDeckListEntry>> GetDeckListAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && _cachedList != null)
            return _cachedList;

        if (!forceRefresh && File.Exists(_cachePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_cachePath, ct);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.LogStuff("Cached DeckList empty.", LogLevel.Warning);
                }
                else
                {
                    var root = JsonSerializer.Deserialize<MtgJsonDeckListRoot>(json, JsonOptions);
                    _cachedList = root?.Data ?? [];
                    if (_cachedList.Count > 0)
                        return _cachedList;
                    Logger.LogStuff("Cached DeckList had no data entries; will re-download.", LogLevel.Warning);
                    try { File.Delete(_cachePath); } catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                Logger.LogStuff($"Failed to read cached DeckList: {ex.Message}", LogLevel.Warning);
                try { File.Delete(_cachePath); } catch { /* ignore */ }
            }
        }

        await DownloadAndCacheDeckListAsync(ct);
        return _cachedList ?? [];
    }

    private async Task DownloadAndCacheDeckListAsync(CancellationToken ct)
    {
        // Try zip first, then fall back to raw JSON (more reliable on some networks/devices).
        var json = await TryDownloadZipAsync(ct);
        if (string.IsNullOrEmpty(json))
            json = await TryDownloadJsonAsync(ct);

        if (string.IsNullOrEmpty(json))
        {
            _cachedList = [];
            return;
        }

        try
        {
            var root = JsonSerializer.Deserialize<MtgJsonDeckListRoot>(json, JsonOptions);
            _cachedList = root?.Data ?? [];
            if (_cachedList.Count == 0)
                Logger.LogStuff("DeckList parsed but data array empty.", LogLevel.Warning);
            await File.WriteAllTextAsync(_cachePath, json, ct);
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"Failed to parse DeckList: {ex.Message}", LogLevel.Error);
            _cachedList = [];
        }
    }

    private async Task<string?> TryDownloadZipAsync(CancellationToken ct)
    {
        try
        {
            using var client = NetworkHelper.CreateHttpClient(TimeSpan.FromSeconds(60));
            using var response = await client.GetAsync(MTGConstants.MTGJsonDeckListUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var zipStream = await response.Content.ReadAsStreamAsync(ct);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // Zip may have entry name "DeckList.json" or path "v5/DeckList.json" etc.
            var entry = archive.Entries.FirstOrDefault(e =>
                e.Name.Equals("DeckList.json", StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                var names = string.Join(", ", archive.Entries.Take(5).Select(e => e.FullName));
                if (archive.Entries.Count > 5) names += ", ...";
                Logger.LogStuff($"DeckList.json not in zip. Entry names: [{names}]", LogLevel.Warning);
                return null;
            }

            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            return await reader.ReadToEndAsync(ct);
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"DeckList zip download failed: {ex.Message}", LogLevel.Warning);
            return null;
        }
    }

    private async Task<string?> TryDownloadJsonAsync(CancellationToken ct)
    {
        try
        {
            using var client = NetworkHelper.CreateHttpClient(TimeSpan.FromSeconds(60));
            var json = await client.GetStringAsync(MTGConstants.MTGJsonDeckListJsonUrl, ct);
            if (!string.IsNullOrWhiteSpace(json))
                Logger.LogStuff("DeckList loaded via direct JSON URL.", LogLevel.Info);
            return json;
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"DeckList JSON download failed: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    /// <summary>
    /// Fetches a single deck by its fileName (e.g. "Commander_2021_Arcane_Maelstrom_C21").
    /// </summary>
    public async Task<MtgJsonDeck?> GetDeckAsync(string fileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var name = fileName.TrimEnd();
        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            name += ".json";

        var url = MTGConstants.MTGJsonDeckBaseUrl + name;
        try
        {
            using var client = NetworkHelper.CreateHttpClient(TimeSpan.FromSeconds(30));
            var json = await client.GetStringAsync(url, ct);
            // API returns {"data": { "name", "mainBoard", ... }} — unwrap to get the deck
            var root = JsonSerializer.Deserialize<MtgJsonDeckRoot>(json, JsonOptions);
            return root?.Data ?? JsonSerializer.Deserialize<MtgJsonDeck>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"Failed to fetch deck {fileName}: {ex.Message}", LogLevel.Warning);
            return null;
        }
    }
}
