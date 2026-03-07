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
                var root = JsonSerializer.Deserialize<MtgJsonDeckListRoot>(json, JsonOptions);
                _cachedList = root?.Data ?? [];
                return _cachedList;
            }
            catch (Exception ex)
            {
                Logger.LogStuff($"Failed to read cached DeckList: {ex.Message}", LogLevel.Warning);
            }
        }

        await DownloadAndCacheDeckListAsync(ct);
        return _cachedList ?? [];
    }

    private async Task DownloadAndCacheDeckListAsync(CancellationToken ct)
    {
        try
        {
            using var client = NetworkHelper.CreateHttpClient(TimeSpan.FromSeconds(60));
            using var response = await client.GetAsync(MTGConstants.MTGJsonDeckListUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var zipStream = await response.Content.ReadAsStreamAsync(ct);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var entry = archive.GetEntry("DeckList.json") ?? archive.Entries.FirstOrDefault(e =>
                e.Name.Equals("DeckList.json", StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                Logger.LogStuff("DeckList.json not found in zip.", LogLevel.Error);
                _cachedList = [];
                return;
            }

            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var json = await reader.ReadToEndAsync(ct);
            var root = JsonSerializer.Deserialize<MtgJsonDeckListRoot>(json, JsonOptions);
            _cachedList = root?.Data ?? [];

            await File.WriteAllTextAsync(_cachePath, json, ct);
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"Failed to download DeckList: {ex.Message}", LogLevel.Error);
            _cachedList = [];
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
            return JsonSerializer.Deserialize<MtgJsonDeck>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"Failed to fetch deck {fileName}: {ex.Message}", LogLevel.Warning);
            return null;
        }
    }
}
