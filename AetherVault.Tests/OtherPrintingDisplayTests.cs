using AetherVault.Core;
using AetherVault.Models;
using AetherVault.Services;

namespace AetherVault.Tests;

public sealed class OtherPrintingDisplayTests
{
    [Fact]
    public void BuildStrip_ReturnsAllWhenUnderCap()
    {
        var rows = Enumerable.Range(0, 10)
            .Select(i => new OtherPrintingSummary { SetCode = $"s{i}" })
            .ToList();

        var strip = OtherPrintingDisplay.BuildStrip(rows);

        Assert.Equal(10, strip.Count);
    }

    [Fact]
    public void BuildStrip_CapsAtStripMax()
    {
        var rows = Enumerable.Range(0, 20)
            .Select(i => new OtherPrintingSummary { SetCode = $"s{i}" })
            .ToList();

        var strip = OtherPrintingDisplay.BuildStrip(rows);

        Assert.Equal(OtherPrintingDisplay.StripMax, strip.Count);
        Assert.Equal("s0", strip[0].SetCode);
    }

    [Fact]
    public void ShouldOfferSeeAll_UsesThreshold()
    {
        Assert.False(OtherPrintingDisplay.ShouldOfferSeeAll(14));
        Assert.True(OtherPrintingDisplay.ShouldOfferSeeAll(15));
        Assert.True(OtherPrintingDisplay.ShouldOfferSeeAll(40));
    }
}
