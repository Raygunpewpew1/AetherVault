using AetherVault.Controls;

namespace AetherVault.Services;

/// <summary>
/// Loads price data for visible card range in a CardGrid and updates the grid in bulk.
/// Shared by SearchViewModel and CollectionViewModel to avoid duplication.
/// </summary>
public interface IGridPriceLoadService
{
    /// <summary>
    /// Fetches prices for cards in the given visible range that lack PriceData,
    /// then updates the grid on the main thread. No-op if grid is null or no UUIDs need loading.
    /// </summary>
    /// <param name="isCollectionGrid">When true, respects <see cref="PricePreferences.CollectionPriceDisplayEnabled"/>.</param>
    void LoadVisiblePrices(CardGrid? grid, int start, int end, bool isCollectionGrid = false);
}
