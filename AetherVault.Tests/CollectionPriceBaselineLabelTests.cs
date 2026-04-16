using AetherVault.Services;

namespace AetherVault.Tests;

public sealed class CollectionPriceBaselineLabelTests
{
    private static readonly PriceVendor[] TcgOnly = [PriceVendor.TcgPlayer];
    private static CardPriceData MakeUsdNormal(double unit)
    {
        var vp = new VendorPrices
        {
            RetailNormal = new PriceEntry(DateTime.UtcNow, unit),
            RetailFoil = PriceEntry.Empty,
            RetailEtched = PriceEntry.Empty,
            BuylistNormal = PriceEntry.Empty,
            BuylistEtched = PriceEntry.Empty,
            Currency = PriceCurrency.Usd,
        };
        return new CardPriceData
        {
            Uuid = "test-uuid",
            Paper = new PaperPlatform { TcgPlayer = vp },
            LastUpdated = DateTime.UtcNow,
        };
    }

    [Fact]
    public void FormatCollectionPriceChangeLabel_NoBaseline_ReturnsEmpty()
    {
        var cur = MakeUsdNormal(5);
        Assert.Equal("", PriceDisplayHelper.FormatCollectionPriceChangeLabel(null, cur, false, false, TcgOnly));
        Assert.Equal("", PriceDisplayHelper.FormatCollectionPriceChangeLabel(0, cur, false, false, TcgOnly));
    }

    [Fact]
    public void FormatCollectionPriceChangeLabel_RoundTripPercent()
    {
        var cur = MakeUsdNormal(11);
        Assert.Equal("+10%", PriceDisplayHelper.FormatCollectionPriceChangeLabel(10, cur, false, false, TcgOnly));
    }

    [Fact]
    public void FormatCollectionPriceChangeLabel_NearZero_ShowsZeroPercent()
    {
        var cur = MakeUsdNormal(10.02);
        Assert.Equal("0%", PriceDisplayHelper.FormatCollectionPriceChangeLabel(10, cur, false, false, TcgOnly));
    }

    [Fact]
    public void FormatCollectionPriceChangeLabel_Decrease_IsNegative()
    {
        var cur = MakeUsdNormal(8);
        Assert.Equal("-20%", PriceDisplayHelper.FormatCollectionPriceChangeLabel(10, cur, false, false, TcgOnly));
    }
}
