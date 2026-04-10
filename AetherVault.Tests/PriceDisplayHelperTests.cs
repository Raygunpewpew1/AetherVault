using AetherVault.Models;
using AetherVault.Services;

namespace AetherVault.Tests;

public class PriceDisplayHelperTests
{
    /// <summary>Matches <see cref="PriceDisplayHelper"/> default order without touching MAUI Preferences.</summary>
    private static readonly PriceVendor[] DefaultVendorOrder =
        [PriceVendor.TcgPlayer, PriceVendor.Cardmarket, PriceVendor.CardKingdom, PriceVendor.ManaPool];

    private static CardPriceData TcgNormalAndFoil(double normal, double foil) => new()
    {
        Paper = new PaperPlatform
        {
            TcgPlayer = VendorPrices.Empty with
            {
                RetailNormal = new PriceEntry(DateTime.UtcNow, normal),
                RetailFoil = new PriceEntry(DateTime.UtcNow, foil),
            }
        }
    };

    [Fact]
    public void GetNumericPrice_UsesFoilWhenRequested()
    {
        var data = TcgNormalAndFoil(5, 25);
        Assert.Equal(25, PriceDisplayHelper.GetNumericPrice(data, isFoil: true, isEtched: false, DefaultVendorOrder));
        Assert.Equal(5, PriceDisplayHelper.GetNumericPrice(data, isFoil: false, isEtched: false, DefaultVendorOrder));
    }

    [Fact]
    public void GetDeckLinePriceDisplay_MultipliesQuantity()
    {
        var data = TcgNormalAndFoil(2.5, 0);
        Assert.Equal("$7.50", PriceDisplayHelper.GetDeckLinePriceDisplay(data, 3));
        Assert.Equal("$2.50", PriceDisplayHelper.GetDeckUnitPriceDisplay(data));
    }

    [Fact]
    public void CollectionPriceSort_OrderByDescendingNumericThenName_MatchesExpected()
    {
        var cheap = TcgNormalAndFoil(1, 0);
        var mid = TcgNormalAndFoil(10, 0);
        var pricey = TcgNormalAndFoil(100, 0);
        var map = new Dictionary<string, CardPriceData>
        {
            ["a"] = mid,
            ["b"] = pricey,
            ["c"] = cheap,
        };

        var items = new[]
        {
            new CollectionItem { CardUuid = "a", Card = new Card { Name = "Mid" } },
            new CollectionItem { CardUuid = "b", Card = new Card { Name = "Top" } },
            new CollectionItem { CardUuid = "c", Card = new Card { Name = "Low" } },
        };

        var ordered = items
            .OrderByDescending(i => PriceDisplayHelper.GetNumericPrice(map.GetValueOrDefault(i.CardUuid), i.IsFoil, i.IsEtched, DefaultVendorOrder))
            .ThenBy(i => i.Card.Name, StringComparer.OrdinalIgnoreCase)
            .Select(i => i.CardUuid)
            .ToArray();

        Assert.Equal(["b", "a", "c"], ordered);
    }

    [Fact]
    public void CollectionPriceSort_TieBreaksByName_WhenPricesEqual()
    {
        var same = TcgNormalAndFoil(7, 0);
        var map = new Dictionary<string, CardPriceData>
        {
            ["x"] = same,
            ["y"] = same,
        };

        var items = new[]
        {
            new CollectionItem { CardUuid = "y", Card = new Card { Name = "Yara" } },
            new CollectionItem { CardUuid = "x", Card = new Card { Name = "Anna" } },
        };

        var ordered = items
            .OrderByDescending(i => PriceDisplayHelper.GetNumericPrice(map.GetValueOrDefault(i.CardUuid), i.IsFoil, i.IsEtched, DefaultVendorOrder))
            .ThenBy(i => i.Card.Name, StringComparer.OrdinalIgnoreCase)
            .Select(i => i.CardUuid)
            .ToArray();

        Assert.Equal(["x", "y"], ordered);
    }
}
