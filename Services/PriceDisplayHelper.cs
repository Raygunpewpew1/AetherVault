namespace AetherVault.Services;

/// <summary>
/// Central helper for resolving display and numeric prices using the user's vendor priority.
/// Reads/writes vendor order via Preferences; used by grid, card detail, and collection total.
/// </summary>
public static class PriceDisplayHelper
{
    private const string VendorPriorityKey = "PriceVendorPriority";
    private const string DefaultOrder = "TCGPlayer,Cardmarket,CardKingdom,ManaPool";

    private static readonly PriceVendor[] DefaultVendorOrder =
    [PriceVendor.TcgPlayer, PriceVendor.Cardmarket, PriceVendor.CardKingdom, PriceVendor.ManaPool];

    /// <summary>
    /// Gets the current vendor priority order from preferences.
    /// </summary>
    public static PriceVendor[] GetVendorPriority()
    {
        var stored = Preferences.Default.Get(VendorPriorityKey, DefaultOrder);
        if (string.IsNullOrWhiteSpace(stored)) return DefaultVendorOrder;

        var names = stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<PriceVendor>();
        foreach (var name in names)
        {
            if (Enum.TryParse<PriceVendor>(name, ignoreCase: true, out var v))
                result.Add(v);
        }

        // Ensure all vendors appear; append any missing in default order
        foreach (var v in DefaultVendorOrder)
        {
            if (!result.Contains(v))
                result.Add(v);
        }

        return [.. result];
    }

    /// <summary>
    /// Sets the preferred (first) vendor; remaining vendors follow in default order.
    /// </summary>
    public static void SetPreferredVendor(PriceVendor first)
    {
        var rest = DefaultVendorOrder.Where(v => v != first).ToArray();
        SetVendorPriority([first, .. rest]);
    }

    /// <summary>
    /// Saves the vendor priority order. Order is used left-to-right when resolving prices.
    /// </summary>
    public static void SetVendorPriority(IReadOnlyList<PriceVendor> order)
    {
        if (order == null || order.Count == 0)
        {
            Preferences.Default.Remove(VendorPriorityKey);
            return;
        }
        var value = string.Join(",", order.Select(v => v.ToString()));
        Preferences.Default.Set(VendorPriorityKey, value);
    }

    /// <summary>
    /// Returns the display price string (e.g. "$12.34") for the first vendor with a valid price,
    /// using the user's vendor priority. Optionally prefers foil/etched for labeling.
    /// </summary>
    public static string GetDisplayPrice(CardPriceData? data, bool preferFoilLabel = false, bool preferEtchedLabel = false)
    {
        if (data == null) return "";
        var (price, isFoil, isEtched, currency) = GetNumericPriceAndFinish(data, false, false, vendorPriorityOverride: null);
        if (price <= 0) return "";

        var suffix = "";
        if (preferEtchedLabel && isEtched) suffix = " (Etched)";
        else if (preferFoilLabel && isFoil) suffix = " (Foil)";

        return currency == PriceCurrency.Eur ? $"€{price:F2}{suffix}" : $"${price:F2}{suffix}";
    }

    /// <summary>
    /// Returns the numeric price for collection total: uses vendor priority and picks
    /// RetailNormal, RetailFoil, or RetailEtched based on the item's finish.
    /// </summary>
    /// <param name="vendorPriorityOverride">When non-null, used instead of app preferences (e.g. unit tests).</param>
    public static double GetNumericPrice(CardPriceData? data, bool isFoil, bool isEtched, PriceVendor[]? vendorPriorityOverride = null)
    {
        if (data == null) return 0;
        var (price, _, _, _) = GetNumericPriceAndFinish(data, isFoil, isEtched, vendorPriorityOverride);
        return price;
    }

    /// <summary>
    /// Percent change from a stored per-row baseline (USD) to the current preferred retail unit price.
    /// Returns empty when baseline is unknown or current price is unavailable.
    /// </summary>
    public static string FormatCollectionPriceChangeLabel(
        double? baselineUsd,
        CardPriceData? current,
        bool isFoil,
        bool isEtched,
        PriceVendor[]? vendorPriorityOverride = null)
    {
        if (baselineUsd is not double b || b <= 0 || current == null)
            return "";
        var cur = GetNumericPrice(current, isFoil, isEtched, vendorPriorityOverride);
        if (cur <= 0)
            return "";
        var pct = (cur - b) / b * 100.0;
        // Whole-percent labels: treat sub-half-percent moves as flat (avoids "+0%" from tiny float drift).
        if (Math.Abs(pct) < 0.5)
            return "0%";
        if (pct > 0)
            return $"+{pct:F0}%";
        return $"{pct:F0}%";
    }

    /// <summary>Preferred retail unit price for deck rows (non-foil); used for line totals and deck sum.</summary>
    public static bool TryGetPreferredUnitPrice(
        CardPriceData? data,
        bool isFoil,
        bool isEtched,
        out double unitPrice,
        out PriceCurrency currency)
    {
        unitPrice = 0;
        currency = PriceCurrency.Usd;
        if (data == null) return false;
        var (price, _, _, cur) = GetNumericPriceAndFinish(data, isFoil, isEtched, vendorPriorityOverride: null);
        if (price <= 0) return false;
        unitPrice = price;
        currency = cur;
        return true;
    }

    public static string GetDeckUnitPriceDisplay(CardPriceData? data)
    {
        if (!TryGetPreferredUnitPrice(data, false, false, out var unit, out var cur)) return "";
        return cur == PriceCurrency.Eur ? $"€{unit:F2}" : $"${unit:F2}";
    }

    public static string GetDeckLinePriceDisplay(CardPriceData? data, int quantity)
    {
        if (quantity <= 0) return "";
        if (!TryGetPreferredUnitPrice(data, false, false, out var unit, out var cur)) return "";
        double line = unit * quantity;
        return cur == PriceCurrency.Eur ? $"€{line:F2}" : $"${line:F2}";
    }

    private static (double price, bool usedFoil, bool usedEtched, PriceCurrency currency) GetNumericPriceAndFinish(
        CardPriceData data, bool preferFoil, bool preferEtched, PriceVendor[]? vendorPriorityOverride)
    {
        var paper = data.Paper;
        var order = vendorPriorityOverride ?? GetVendorPriority();

        foreach (var vendor in order)
        {
            var v = GetVendorPrices(paper, vendor);
            if (v == null || !v.IsValid) continue;

            var cur = v.Currency;

            // Prefer etched then foil then normal when matching collection item finish
            if (preferEtched && v.RetailEtched.Price > 0)
                return (v.RetailEtched.Price, false, true, cur);
            if (preferFoil && v.RetailFoil.Price > 0)
                return (v.RetailFoil.Price, true, false, cur);
            if (v.RetailNormal.Price > 0)
                return (v.RetailNormal.Price, false, false, cur);
            if (v.RetailFoil.Price > 0)
                return (v.RetailFoil.Price, true, false, cur);
            if (v.RetailEtched.Price > 0)
                return (v.RetailEtched.Price, false, true, cur);
        }

        return (0, false, false, PriceCurrency.Usd);
    }

    private static VendorPrices? GetVendorPrices(PaperPlatform paper, PriceVendor vendor)
    {
        return vendor switch
        {
            PriceVendor.TcgPlayer => paper.TcgPlayer,
            PriceVendor.Cardmarket => paper.Cardmarket,
            PriceVendor.CardKingdom => paper.CardKingdom,
            PriceVendor.ManaPool => paper.ManaPool,
            _ => null
        };
    }
}
