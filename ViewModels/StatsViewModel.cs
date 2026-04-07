using AetherVault.Models;
using AetherVault.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;

namespace AetherVault.ViewModels;

/// <summary>
/// ViewModel for statistics page.
/// Displays collection stats and cache information.
/// </summary>
public partial class StatsViewModel : BaseViewModel
{
    private readonly CardManager _cardManager;

    /// <summary>True when stats need to be reloaded (e.g. collection was mutated since last load).</summary>
    public bool IsStatsStale { get; private set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatsDisplay))]
    public partial CollectionStats Stats { get; set; } = new();

    /// <summary>Donut chart for rarity mix; null when collection is empty.</summary>
    [ObservableProperty]
    public partial Chart? RarityDistributionChart { get; set; }

    /// <summary>Donut chart for creature / spell / lands; null when collection is empty.</summary>
    [ObservableProperty]
    public partial Chart? CardTypeDistributionChart { get; set; }

    /// <summary>True when <see cref="RarityDistributionChart"/> and <see cref="CardTypeDistributionChart"/> are shown.</summary>
    [ObservableProperty]
    public partial bool HasCollectionCharts { get; set; }

    [ObservableProperty]
    public partial StorageStats Storage { get; set; } = new();

    [ObservableProperty]
    public partial string CacheStats { get; set; } = "";

    [ObservableProperty]
    public partial string DatabaseStatus { get; set; } = "";

    public string StatsDisplay => Stats.ToString();

    public StatsViewModel(CardManager cardManager)
    {
        _cardManager = cardManager;
        _cardManager.CollectionChanged += InvalidateStats;
    }

    partial void OnStatsChanged(CollectionStats value) => ApplyDistributionCharts(value);

    private void ApplyDistributionCharts(CollectionStats s)
    {
        if (!CollectionStatsCharts.TryCreateDistributionCharts(s, out var rarity, out var type))
        {
            HasCollectionCharts = false;
            RarityDistributionChart = null;
            CardTypeDistributionChart = null;
            return;
        }

        HasCollectionCharts = true;
        RarityDistributionChart = rarity;
        CardTypeDistributionChart = type;
    }

    /// <summary>Marks stats as stale so the next OnAppearing triggers a reload.</summary>
    public void InvalidateStats()
    {
        IsStatsStale = true;
        // Do not reset image-cache / storage snapshot here: every collection edit would re-scan
        // thousands of cache files on the next Stats visit. File sizes still refresh each visit below.
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        InvalidateStats();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        IsBusy = true;
        StatusMessage = UserMessages.ClearingCache;
        try
        {
            await _cardManager.ClearImageCacheAsync();
            CacheStats = await _cardManager.GetImageCacheStatsAsync();
            Storage.ImageCacheSize = _cardManager.ImageService.Cache.GetTotalCacheSize();
            OnPropertyChanged(nameof(Storage)); // notify UI about the total size update
            StatusMessage = UserMessages.CacheCleared;
        }
        catch (Exception ex)
        {
            StatusMessage = UserMessages.Error(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task LoadStatsAsync()
    {
        // Show placeholders immediately so the page doesn't block or lag
        IsBusy = true;
        CacheStats = "…";
        Storage = new StorageStats();
        IsBusy = false;
        IsStatsStale = false;

        // Launch all three independent tasks in parallel — none depends on the others
        _ = LoadStatsInBackgroundAsync();
        _ = LoadTotalValueInBackgroundAsync();
        _ = LoadStorageAndCacheInBackgroundAsync();
        return Task.CompletedTask;
    }

    /// <summary>Refreshes only the total collection value (e.g. after changing the price vendor).</summary>
    public Task RefreshTotalValueAsync()
    {
        _ = LoadTotalValueInBackgroundAsync();
        return Task.CompletedTask;
    }

    private async Task LoadStatsInBackgroundAsync()
    {
        try
        {
            if (!await _cardManager.EnsureInitializedAsync())
            {
                MainThread.BeginInvokeOnMainThread(() => DatabaseStatus = "Database not connected");
                return;
            }

            var stats = await _cardManager.GetCollectionStatsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Stats = stats;
                DatabaseStatus = "Connected";
                OnPropertyChanged(nameof(Stats));
            });
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"Stats load error: {ex.Message}", LogLevel.Error);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DatabaseStatus = $"Error: {ex.Message}";
            });
        }
    }

    private async Task LoadTotalValueInBackgroundAsync()
    {
        try
        {
            if (!PricePreferences.PricesDataEnabled || !PricePreferences.CollectionPriceDisplayEnabled)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var s = Stats;
                    s.TotalValue = 0;
                    Stats = s;
                    OnPropertyChanged(nameof(Stats));
                });
                return;
            }

            var total = await _cardManager.GetCollectionTotalValueAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var s = Stats;
                s.TotalValue = total;
                Stats = s;
                OnPropertyChanged(nameof(Stats));
            });
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"Total value load failed: {ex.Message}", LogLevel.Warning);
        }
    }

    private async Task LoadStorageAndCacheInBackgroundAsync()
    {
        try
        {
            var cacheStats = await _cardManager.GetImageCacheStatsAsync();
            var mtgSize = GetFileSize(AppDataManager.GetMtgDatabasePath());
            var collSize = GetFileSize(AppDataManager.GetCollectionDatabasePath());
            var pricesSize = GetFileSize(AppDataManager.GetPricesDatabasePath());
            var cacheSize = _cardManager.ImageService.Cache.GetTotalCacheSize();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CacheStats = cacheStats;
                Storage = new StorageStats
                {
                    MtgDatabaseSize = mtgSize,
                    CollectionDatabaseSize = collSize,
                    PricesDatabaseSize = pricesSize,
                    ImageCacheSize = cacheSize
                };
                OnPropertyChanged(nameof(Storage));
            });
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"Storage stats load failed: {ex.Message}", LogLevel.Warning);
            MainThread.BeginInvokeOnMainThread(() => CacheStats = "—");
        }
    }

    private long GetFileSize(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                // Ignore permissions/locking issues and just return 0
            }
        }
        return 0;
    }
}
