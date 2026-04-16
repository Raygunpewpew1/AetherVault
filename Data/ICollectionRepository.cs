using AetherVault.Models;

namespace AetherVault.Data;

/// <summary>
/// Async interface for user collection data access.
/// Port of ICollectionRepository from CollectionRepository.pas.
/// </summary>
public interface ICollectionRepository
{
    Task AddCardAsync(string cardUuid, int quantity = 1, bool isFoil = false, bool isEtched = false);
    Task AddCardsBulkAsync(IEnumerable<(string cardUUID, int quantity, bool isFoil, bool isEtched)> cards);
    Task RemoveCardAsync(string cardUuid);
    Task ClearCollectionAsync();
    Task UpdateQuantityAsync(string cardUuid, int quantity, bool isFoil = false, bool isEtched = false);
    Task<CollectionItem[]> GetCollectionAsync();
    /// <summary>Lightweight list of (uuid, quantity, isFoil, isEtched) for pricing total. No Card load.</summary>
    Task<IReadOnlyList<(string Uuid, int Quantity, bool IsFoil, bool IsEtched)>> GetPricingEntriesAsync();
    Task<CollectionStats> GetCollectionStatsAsync();
    Task<bool> IsInCollectionAsync(string cardUuid);
    Task<int> GetQuantityAsync(string cardUuid);
    /// <summary>Returns owned quantity per UUID (0 for cards not in collection).</summary>
    Task<Dictionary<string, int>> GetQuantitiesAsync(IEnumerable<string> cardUuids);
    Task ReorderAsync(IList<string> orderedUuids);

    /// <summary>Returns foil/etched flags for an owned row, or null when the UUID is not in the collection.</summary>
    Task<(bool IsFoil, bool IsEtched)?> TryGetFinishFlagsAsync(string cardUuid);

    /// <summary>Stores a price baseline when the row has none (NULL or &lt;= 0).</summary>
    Task TrySetReferenceBaselineIfMissingAsync(string cardUuid, double unitPriceUsd, DateTime capturedUtc);

    /// <summary>Overwrites the stored baseline (e.g. user-initiated recapture).</summary>
    Task SetReferenceBaselineAsync(string cardUuid, double unitPriceUsd, DateTime capturedUtc);
}
