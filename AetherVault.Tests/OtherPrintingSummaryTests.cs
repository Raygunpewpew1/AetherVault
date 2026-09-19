using AetherVault.Core;
using AetherVault.Models;

namespace AetherVault.Tests;

public sealed class OtherPrintingSummaryTests
{
    [Fact]
    public void TitleText_UsesSetNameWhenPresent()
    {
        var row = new OtherPrintingSummary
        {
            SetName = "Magic 2011",
            SetCode = "m11"
        };

        Assert.Equal("Magic 2011", row.TitleText);
    }

    [Fact]
    public void TitleText_FallsBackToSetCodeWhenNameMissing()
    {
        var row = new OtherPrintingSummary { SetCode = "lea" };

        Assert.Equal("LEA", row.TitleText);
    }

    [Fact]
    public void DetailText_IncludesSetCodeRarityAndYear()
    {
        var row = new OtherPrintingSummary
        {
            SetCode = "m11",
            Rarity = CardRarity.Rare,
            SetReleaseDate = "2010-07-16"
        };

        Assert.Equal("M11 · Rare · 2010", row.DetailText);
    }

    [Fact]
    public void DetailText_OmitsYearWhenReleaseDateMissing()
    {
        var row = new OtherPrintingSummary
        {
            SetCode = "mh2",
            Rarity = CardRarity.Mythic
        };

        Assert.Equal("MH2 · Mythic", row.DetailText);
    }

    [Fact]
    public void OwnershipBadge_ShowsQuantityAndFoil()
    {
        var row = new OtherPrintingSummary
        {
            CollectionQuantity = 3,
            IsFoilOwned = true
        };

        Assert.Equal("×3 · foil", row.OwnershipBadge);
        Assert.True(row.ShowOwnershipBadge);
    }

    [Fact]
    public void SearchHaystack_IncludesSetFields()
    {
        var row = new OtherPrintingSummary
        {
            SetName = "Magic 2011",
            SetCode = "m11",
            Number = "149",
            Rarity = CardRarity.Common
        };

        Assert.Contains("magic 2011", row.SearchHaystack);
        Assert.Contains("m11", row.SearchHaystack);
        Assert.Contains("149", row.SearchHaystack);
    }
}
