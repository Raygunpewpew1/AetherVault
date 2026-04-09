using AetherVault.Services;

namespace AetherVault.Tests;

/// <summary>
/// Vendor key string shape must stay aligned with <c>CollectionViewModel</c> sort cache and
/// <c>CardManager</c> warm prefetch (<see cref="PriceDisplayHelper.GetVendorPriority"/> order).
/// </summary>
public class CollectionPriceVendorKeyTests
{
    private static string VendorKeyFromPriority(PriceVendor[] p) =>
        p.Length == 0 ? "" : string.Join(',', p.Select(static v => v.ToString()));

    [Fact]
    public void Empty_priority_yields_empty_string()
    {
        Assert.Equal("", VendorKeyFromPriority([]));
    }

    [Fact]
    public void Non_empty_priority_joins_enum_names_with_commas()
    {
        var p = new[] { PriceVendor.TcgPlayer, PriceVendor.Cardmarket };
        Assert.Equal("TcgPlayer,Cardmarket", VendorKeyFromPriority(p));
    }
}
