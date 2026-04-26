using AetherVault.Models;
using AetherVault.Services.DeckBuilder;

namespace AetherVault.Tests;

public class DeckBrowseListNameCollapseTests
{
    [Fact]
    public void ApplyIfNeeded_CollapsesToOnePerName_UsesEdhrecAndSetTiebreak()
    {
        var a = new Card { Name = "Steam Vents", Uuid = "1", SetCode = "RAV", EdhRecRank = 5 };
        var b = new Card { Name = "Steam Vents", Uuid = "2", SetCode = "GRN", EdhRecRank = 0 };
        var c = new Card { Name = "Stomping Ground", Uuid = "3", SetCode = "GTC", EdhRecRank = 1 };

        var all = new[] { b, a, c };
        var result = DeckBrowseListNameCollapse.ApplyIfNeeded("shocks", namePart: null, all);

        Assert.Equal(2, result.Length);
        Assert.Equal("Steam Vents", result[0].Name);
        Assert.Equal("1", result[0].Uuid);
        Assert.Equal("Stomping Ground", result[1].Name);
    }

    [Fact]
    public void ApplyIfNeeded_WhenNamePartSet_PassesThroughUnchanged()
    {
        var cards = new[] { new Card { Name = "X", Uuid = "1" } };
        var result = DeckBrowseListNameCollapse.ApplyIfNeeded("shocks", "Steam", cards);
        Assert.Single(result);
        Assert.Equal("1", result[0].Uuid);
    }
}
