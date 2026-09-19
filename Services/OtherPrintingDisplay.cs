using AetherVault.Models;

namespace AetherVault.Services;

/// <summary>Strip cap and "see all" threshold for card-detail other printings.</summary>
public static class OtherPrintingDisplay
{
    public const int StripMax = 12;
    public const int SeeAllThreshold = 15;

    public static List<OtherPrintingSummary> BuildStrip(IReadOnlyList<OtherPrintingSummary> ordered) =>
        ordered.Count <= StripMax ? [.. ordered] : ordered.Take(StripMax).ToList();

    public static bool ShouldOfferSeeAll(int totalCount) => totalCount >= SeeAllThreshold;
}
