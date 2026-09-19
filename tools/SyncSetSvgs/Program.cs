using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherVault.Tools.SyncSetSvgs;

/// <summary>
/// Downloads missing MTG set symbol SVGs from Scryfall into Assets/SVGSets.
/// Filenames match <c>SetSvgCache</c>: lowercase set code → {code}.svg
/// </summary>
/// <example>
/// dotnet run --project tools/SyncSetSvgs
/// dotnet run --project tools/SyncSetSvgs -- --force
/// dotnet run --project tools/SyncSetSvgs -- --dry-run
/// </example>
internal static class Program
{
    private const string SetsUrl = "https://api.scryfall.com/sets";
    private const string UserAgent = "AetherVault/1.0 (set-svg-sync; personal project)";
    private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(120);

    private static async Task<int> Main(string[] args)
    {
        bool force = args.Contains("--force", StringComparer.OrdinalIgnoreCase);
        bool dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);

        string? outDirArg = GetOptionValue(args, "--out");
        string repoRoot = FindRepoRoot();
        string outDir = string.IsNullOrWhiteSpace(outDirArg)
            ? Path.Combine(repoRoot, "Assets", "SVGSets")
            : Path.GetFullPath(outDirArg);

        if (!Directory.Exists(outDir))
        {
            Console.Error.WriteLine($"Output directory not found: {outDir}");
            return 1;
        }

        using var http = CreateHttpClient();

        Console.WriteLine("Fetching set list from Scryfall…");
        var list = await FetchSetsAsync(http);
        Console.WriteLine($"Scryfall returned {list.Count} sets.");

        int added = 0, existing = 0, skipped = 0, failed = 0;

        foreach (var set in list)
        {
            if (string.IsNullOrWhiteSpace(set.Code))
            {
                skipped++;
                continue;
            }

            string code = set.Code.Trim().ToLowerInvariant();
            if (code is "fallback")
            {
                skipped++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(set.IconSvgUri))
            {
                Console.Error.WriteLine($"No icon_svg_uri for set '{code}' ({set.Name})");
                failed++;
                continue;
            }

            string dest = Path.Combine(outDir, $"{code}.svg");
            if (File.Exists(dest) && !force)
            {
                existing++;
                continue;
            }

            if (dryRun)
            {
                Console.WriteLine($"[dry-run] would download {code} ← {set.IconSvgUri}");
                added++;
                continue;
            }

            try
            {
                string svg = await http.GetStringAsync(set.IconSvgUri);
                if (svg.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Response did not look like SVG");

                await File.WriteAllTextAsync(dest, svg.Trim() + "\n");
                Console.WriteLine($"Added {code}.svg ({set.Name})");
                added++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed {code} ({set.Name}): {ex.Message}");
                failed++;
            }

            await Task.Delay(RequestDelay);
        }

        Console.WriteLine();
        Console.WriteLine($"Done. added={added} existing={existing} skipped={skipped} failed={failed} out={outDir}");
        return failed > 0 ? 1 : 0;
    }

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return http;
    }

    private static async Task<IReadOnlyList<ScryfallSet>> FetchSetsAsync(HttpClient http)
    {
        await using var stream = await http.GetStreamAsync(SetsUrl);
        var page = await JsonSerializer.DeserializeAsync(stream, SyncSetSvgsJsonContext.Default.ScryfallSetList)
            ?? throw new InvalidOperationException("Empty Scryfall /sets response");

        if (page.HasMore)
            Console.Error.WriteLine("Warning: Scryfall reported has_more=true; only the first page was loaded.");

        return page.Data ?? [];
    }

    private static string FindRepoRoot()
    {
        // tools/SyncSetSvgs/bin/.../ → walk up until Assets/SVGSets exists
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            string candidate = Path.Combine(dir, "Assets", "SVGSets");
            if (Directory.Exists(candidate))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        // Fallback: tools/SyncSetSvgs → repo root
        string fromProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        if (Directory.Exists(Path.Combine(fromProject, "Assets", "SVGSets")))
            return fromProject;

        throw new InvalidOperationException("Could not locate repo root (Assets/SVGSets). Pass --out <path>.");
    }

    private static string? GetOptionValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}

internal sealed class ScryfallSetList
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("data")]
    public List<ScryfallSet>? Data { get; set; }
}

internal sealed class ScryfallSet
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("icon_svg_uri")]
    public string? IconSvgUri { get; set; }
}

[JsonSerializable(typeof(ScryfallSetList))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class SyncSetSvgsJsonContext : JsonSerializerContext;
