using AetherVault.Core;
using AetherVault.Data;
using AetherVault.Models;

namespace AetherVault.Tests;

public sealed class CollectionFilterHelperTests
{
    [Fact]
    public void BaseCollection_UnionsTokensWithCards()
    {
        Assert.Contains("UNION ALL", SqlQueries.BaseCollection, StringComparison.Ordinal);
        Assert.Contains("FROM tokens c", SqlQueries.BaseCollection, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INNER JOIN col.my_collection mc", SqlQueries.BaseCollection, StringComparison.Ordinal);
    }

    [Fact]
    public void IntersectPreservingOrder_KeepsOriginalOrderAndSkipsMissing()
    {
        var all = new[]
        {
            new CollectionItem { CardUuid = "a", Card = new Card { Name = "A" } },
            new CollectionItem { CardUuid = "b", Card = new Card { Name = "B" } },
            new CollectionItem { CardUuid = "c", Card = new Card { Name = "C" } },
            new CollectionItem { CardUuid = "d", Card = new Card { Name = "D" } },
        };

        var allowed = new HashSet<string>(["c", "a"], StringComparer.Ordinal);

        var filtered = CollectionFilterHelper.IntersectPreservingOrder(all, allowed);

        Assert.Equal(2, filtered.Length);
        Assert.Equal("a", filtered[0].CardUuid);
        Assert.Equal("c", filtered[1].CardUuid);
    }

    [Fact]
    public void IntersectPreservingOrder_EmptyAllowed_ReturnsEmpty()
    {
        var all = new[] { new CollectionItem { CardUuid = "x" } };
        var filtered = CollectionFilterHelper.IntersectPreservingOrder(all, new HashSet<string>());
        Assert.Empty(filtered);
    }

    [Fact]
    public void ApplyRowFilters_FoilOnly_KeepsFoilLines()
    {
        var items = new[]
        {
            new CollectionItem { CardUuid = "a", IsFoil = false, Quantity = 4 },
            new CollectionItem { CardUuid = "b", IsFoil = true, Quantity = 1 },
        };
        var r = CollectionFilterHelper.ApplyRowFilters(items, foilOnly: true, etchedOnly: false, minQuantityInclusive: 0);
        Assert.Single(r);
        Assert.Equal("b", r[0].CardUuid);
    }

    [Fact]
    public void ApplyRowFilters_MinQuantity_FiltersByQuantity()
    {
        var items = new[]
        {
            new CollectionItem { CardUuid = "a", Quantity = 2 },
            new CollectionItem { CardUuid = "b", Quantity = 4 },
        };
        var r = CollectionFilterHelper.ApplyRowFilters(items, false, false, minQuantityInclusive: 3);
        Assert.Single(r);
        Assert.Equal("b", r[0].CardUuid);
    }
}
