using System.Text.RegularExpressions;
using AetherVault.Constants;

namespace AetherVault.Services.ImportExport;

/// <summary>
/// Imports decks from supported third-party URLs (public Moxfield decks).
/// </summary>
public sealed class DeckUrlImporter
{
    private static readonly Regex MoxfieldDeckUrl = new(
        @"https?://(?:www\.)?moxfield\.com/decks/(?<id>[a-zA-Z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ManaBoxDeckUrl = new(
        @"https?://(?:www\.)?manabox\.app/decks/(?<id>[a-zA-Z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly DeckImporter _deckImporter;

    public DeckUrlImporter(DeckImporter deckImporter)
    {
        _deckImporter = deckImporter;
    }

    public async Task<DeckImportResult> ImportFromUrlAsync(
        string url,
        Action<string, int>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var trimmed = (url ?? "").Trim();
        if (trimmed.Length == 0)
        {
            var empty = new DeckImportResult();
            empty.Errors.Add("No URL entered.");
            return empty;
        }

        if (ManaBoxDeckUrl.IsMatch(trimmed))
        {
            var r = new DeckImportResult();
            r.Errors.Add(UserMessages.ManaBoxDeckUrlHint);
            return r;
        }

        if (!TryParseMoxfieldDeckUrl(trimmed, out var moxId))
        {
            var r = new DeckImportResult();
            r.Errors.Add(UserMessages.UnsupportedDeckUrlHint);
            return r;
        }

        onProgress?.Invoke("Downloading deck from Moxfield...", 0);

        using var http = NetworkHelper.CreateHttpClient(TimeSpan.FromSeconds(45));
        var requestUri = $"https://api2.moxfield.com/v3/decks/all/{Uri.EscapeDataString(moxId)}";
        using var response = await http.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var fail = new DeckImportResult();
            fail.Errors.Add($"Moxfield returned {(int)response.StatusCode}. The deck may be private or removed.");
            return fail;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!MoxfieldDeckJsonParser.TryBuildRows(json, out var deckName, out var formatText, out var rows, out var parseError))
        {
            var fail = new DeckImportResult();
            fail.Errors.Add(parseError ?? "Could not read Moxfield deck.");
            return fail;
        }

        onProgress?.Invoke("Importing cards...", 0);
        return await _deckImporter
            .ImportPreparedDeckAsync(deckName ?? "Imported deck", formatText, rows, onProgress)
            .ConfigureAwait(false);
    }

    /// <summary>Extracts Moxfield public id from a deck URL (for tests and call sites).</summary>
    public static bool TryParseMoxfieldDeckUrl(string? input, out string publicId)
    {
        publicId = "";
        if (string.IsNullOrWhiteSpace(input))
            return false;
        var m = MoxfieldDeckUrl.Match(input.Trim());
        if (!m.Success)
            return false;
        publicId = m.Groups["id"].Value;
        return publicId.Length > 0;
    }
}
