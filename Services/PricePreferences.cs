namespace AetherVault.Services;

/// <summary>
/// User preferences for price downloads, card-price UI, and collection-specific price display.
/// </summary>
public static class PricePreferences
{
    private const string PricesDataEnabledKey = "prices_data_enabled";
    private const string CollectionPriceDisplayEnabledKey = "collection_price_display_enabled";
    private const string SyncPendingKey = "prices_sync_pending";

    /// <summary>When false, no MTGJSON price DB download/sync and no card price lookups.</summary>
    public static bool PricesDataEnabled
    {
        get => Preferences.Default.Get(PricesDataEnabledKey, true);
        set => Preferences.Default.Set(PricesDataEnabledKey, value);
    }

    /// <summary>
    /// When false (but <see cref="PricesDataEnabled"/> is true), hides collection total value,
    /// collection grid prices, and related Stats UI. Search and card detail still show prices.
    /// </summary>
    public static bool CollectionPriceDisplayEnabled
    {
        get => Preferences.Default.Get(CollectionPriceDisplayEnabledKey, true);
        set => Preferences.Default.Set(CollectionPriceDisplayEnabledKey, value);
    }

    /// <summary>Set during price download/sync; used to nudge a retry after an interrupted run.</summary>
    public static bool SyncPending => Preferences.Default.Get(SyncPendingKey, false);

    public static void SetSyncPending(bool pending) => Preferences.Default.Set(SyncPendingKey, pending);

    /// <summary>Raised when display-affecting price preferences change (e.g. from Settings).</summary>
    public static event EventHandler? PriceDisplayPreferencesChanged;

    public static void NotifyPriceDisplayPreferencesChanged() =>
        PriceDisplayPreferencesChanged?.Invoke(null, EventArgs.Empty);
}
