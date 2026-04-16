using AetherVault.Core;

namespace AetherVault.Services.DeckBuilder;

public sealed record DeckBrowseListChipItem(string Key, string DisplayText);

/// <summary>
/// Curated "everyone searches for these" lists for the deck add-cards sheet (exact-name cycles + a few rules-based picks).
/// </summary>
public static class DeckBrowseListCatalog
{
    private static SearchOptions Base() => new() { PrimarySideOnly = true };

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

    private static SearchOptions Shocks()
    {
        var o = Base();
        o.NameEqualsAny =
        [
            "Breeding Pool", "Godless Shrine", "Hallowed Fountain", "Overgrown Tomb", "Sacred Foundry",
            "Steam Vents", "Stomping Ground", "Temple Garden", "Watery Grave", "Blood Crypt",
        ];
        return o;
    }

    private static SearchOptions Fetches()
    {
        var o = Base();
        o.NameEqualsAny =
        [
            "Arid Mesa", "Bloodstained Mire", "Flooded Strand", "Marsh Flats", "Misty Rainforest",
            "Polluted Delta", "Scalding Tarn", "Verdant Catacombs", "Wooded Foothills", "Windswept Heath",
        ];
        return o;
    }

    private static SearchOptions OriginalDuals()
    {
        var o = Base();
        o.NameEqualsAny =
        [
            "Tundra", "Underground Sea", "Badlands", "Taiga", "Savannah", "Scrubland", "Volcanic Island",
            "Bayou", "Tropical Island", "Plateau",
        ];
        return o;
    }

    private static SearchOptions FastLands()
    {
        var o = Base();
        o.NameEqualsAny =
        [
            "Blackcleave Cliffs", "Concealed Courtyard", "Copperline Gorge", "Inspiring Vantage",
            "Botanical Sanctum", "Spirebluff Canal", "Blooming Marsh", "Darkslick Shores", "Seachrome Coast",
            "Razorverge Thicket",
        ];
        return o;
    }

    private static SearchOptions CheckLands()
    {
        var o = Base();
        o.NameEqualsAny =
        [
            "Drowned Catacomb", "Dragonskull Summit", "Glacial Fortress", "Hinterland Harbor", "Isolated Chapel",
            "Rootbound Crag", "Sunpetal Grove", "Sulfur Falls", "Woodland Cemetery", "Clifftop Retreat",
        ];
        return o;
    }

    private static SearchOptions PainLands()
    {
        var o = Base();
        o.NameEqualsAny =
        [
            "Adarkar Wastes", "Battlefield Forge", "Brushland", "Caves of Koilos", "Karplusan Forest",
            "Llanowar Wastes", "Shivan Reef", "Sulfurous Springs", "Underground River", "Yavimaya Coast",
        ];
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
        o.NameEqualsAny =
        [
            "Wasteland", "Strip Mine", "Ghost Quarter", "Field of Ruin", "Tectonic Edge", "Demolition Field",
            "Boseiju, Who Endures", "Otawara, Soaring City", "Minamo, School at Water's Edge", "Shinka, the Bloodsoaked Keep",
        ];
        return o;
    }

    private static SearchOptions ManaRocks()
    {
        var o = Base();
        o.NameEqualsAny =
        [
            "Sol Ring", "Mana Crypt", "Chrome Mox", "Mox Diamond", "Mox Opal", "Jeweled Lotus", "Mana Vault",
            "Grim Monolith", "Basalt Monolith", "Thran Dynamo", "Gilded Lotus", "Coveted Jewel",
        ];
        return o;
    }
}
