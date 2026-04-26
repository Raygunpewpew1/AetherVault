using AetherVault.Core;

namespace AetherVault.Services.DeckBuilder;

public sealed record DeckBrowseListChipItem(string Key, string DisplayText);

/// <summary>
/// Curated "everyone searches for these" lists for the deck add-cards sheet (exact-name cycles + a few rules-based picks).
/// </summary>
public static class DeckBrowseListCatalog
{
    /// <summary>Increment when curated list queries change so <see cref="DeckBrowseListResultCache"/> can invalidate.</summary>
    public const int CacheRevision = 2;

    private static SearchOptions Base() => new() { PrimarySideOnly = true };

    private static readonly string[] ShocksList =
    [
        "Breeding Pool", "Godless Shrine", "Hallowed Fountain", "Overgrown Tomb", "Sacred Foundry",
        "Steam Vents", "Stomping Ground", "Temple Garden", "Watery Grave", "Blood Crypt",
    ];

    private static readonly string[] FetchesList =
    [
        "Arid Mesa", "Bloodstained Mire", "Flooded Strand", "Marsh Flats", "Misty Rainforest",
        "Polluted Delta", "Scalding Tarn", "Verdant Catacombs", "Wooded Foothills", "Windswept Heath",
    ];

    private static readonly string[] OriginalDualsList =
    [
        "Tundra", "Underground Sea", "Badlands", "Taiga", "Savannah", "Scrubland", "Volcanic Island",
        "Bayou", "Tropical Island", "Plateau",
    ];

    private static readonly string[] FastLandsList =
    [
        "Blackcleave Cliffs", "Concealed Courtyard", "Copperline Gorge", "Inspiring Vantage",
        "Botanical Sanctum", "Spirebluff Canal", "Blooming Marsh", "Darkslick Shores", "Seachrome Coast",
        "Razorverge Thicket",
    ];

    private static readonly string[] CheckLandsList =
    [
        "Drowned Catacomb", "Dragonskull Summit", "Glacial Fortress", "Hinterland Harbor", "Isolated Chapel",
        "Rootbound Crag", "Sunpetal Grove", "Sulfur Falls", "Woodland Cemetery", "Clifftop Retreat",
    ];

    private static readonly string[] PainLandsList =
    [
        "Adarkar Wastes", "Battlefield Forge", "Brushland", "Caves of Koilos", "Karplusan Forest",
        "Llanowar Wastes", "Shivan Reef", "Sulfurous Springs", "Underground River", "Yavimaya Coast",
    ];

    private static readonly string[] UtilityLandsList =
    [
        "Wasteland", "Strip Mine", "Ghost Quarter", "Field of Ruin", "Tectonic Edge", "Demolition Field",
        "Boseiju, Who Endures", "Otawara, Soaring City", "Minamo, School at Water's Edge", "Shinka, the Bloodsoaked Keep",
    ];

    private static readonly string[] ManaRocksList =
    [
        "Sol Ring", "Mana Crypt", "Chrome Mox", "Mox Diamond", "Mox Opal", "Jeweled Lotus", "Mana Vault",
        "Grim Monolith", "Basalt Monolith", "Thran Dynamo", "Gilded Lotus", "Coveted Jewel",
    ];

    public static IReadOnlyList<DeckBrowseListChipItem> CreateChipItems() =>
    [
        new("shocks", "Shock lands"),
        new("fetches", "Fetch lands"),
        new("duals", "Original dual lands"),
        new("fastlands", "Fast lands"),
        new("checklands", "Check lands"),
        new("painlands", "Pain lands"),
        new("triomes", "Triomes"),
        new("gamechangers", "Game Changers"),
        new("utility_staples", "Utility lands"),
        new("mana_rocks", "Mana rocks"),
    ];

    public static SearchOptions CreateOptions(string key) => key switch
    {
        "shocks" => Shocks(),
        "fetches" => Fetches(),
        "duals" => OriginalDuals(),
        "fastlands" => FastLands(),
        "checklands" => CheckLands(),
        "painlands" => PainLands(),
        "triomes" => Triomes(),
        "gamechangers" => GameChangers(),
        "utility_staples" => UtilityLands(),
        "mana_rocks" => ManaRocks(),
        _ => Base()
    };

    /// <summary>Order used for one-per-name list display. Null when the list is not a fixed English name table.</summary>
    public static IReadOnlyList<string>? GetEnglishNameListOrderOrNull(string key) => key switch
    {
        "shocks" => ShocksList,
        "fetches" => FetchesList,
        "duals" => OriginalDualsList,
        "fastlands" => FastLandsList,
        "checklands" => CheckLandsList,
        "painlands" => PainLandsList,
        "utility_staples" => UtilityLandsList,
        "mana_rocks" => ManaRocksList,
        _ => null
    };

    public static bool IsEnglishNameListKey(string? key) =>
        key is not null && GetEnglishNameListOrderOrNull(key) is { Count: > 0 };

    /// <summary>Max SQL rows for a quick-browse run (high for English-name lists so all names are present before one-per-name collapse).</summary>
    public static int QuickBrowseSqlRowLimit(string? catalogKey, string? namePart)
    {
        if (!string.IsNullOrEmpty(namePart)) return 50;
        if (string.IsNullOrEmpty(catalogKey)) return 40;
        return IsEnglishNameListKey(catalogKey) ? DeckBrowseListNameCollapse.EnglishNameListSqlOverfetch : 40;
    }

    private static SearchOptions Shocks()
    {
        var o = Base();
        o.NameEqualsAny = [.. ShocksList];
        return o;
    }

    private static SearchOptions Fetches()
    {
        var o = Base();
        o.NameEqualsAny = [.. FetchesList];
        return o;
    }

    private static SearchOptions OriginalDuals()
    {
        var o = Base();
        o.NameEqualsAny = [.. OriginalDualsList];
        return o;
    }

    private static SearchOptions FastLands()
    {
        var o = Base();
        o.NameEqualsAny = [.. FastLandsList];
        return o;
    }

    private static SearchOptions CheckLands()
    {
        var o = Base();
        o.NameEqualsAny = [.. CheckLandsList];
        return o;
    }

    private static SearchOptions PainLands()
    {
        var o = Base();
        o.NameEqualsAny = [.. PainLandsList];
        return o;
    }

    private static SearchOptions Triomes()
    {
        var o = Base();
        o.TypeFilter = "Land";
        o.NameFilter = "Triome";
        return o;
    }

    private static SearchOptions GameChangers()
    {
        var o = Base();
        o.GameChangerOnly = true;
        return o;
    }

    private static SearchOptions UtilityLands()
    {
        var o = Base();
        o.NameEqualsAny = [.. UtilityLandsList];
        return o;
    }

    private static SearchOptions ManaRocks()
    {
        var o = Base();
        o.NameEqualsAny = [.. ManaRocksList];
        return o;
    }
}
