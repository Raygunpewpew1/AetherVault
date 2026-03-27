using AetherVault.Services;
using AetherVault.ViewModels;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace AetherVault.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly StatsViewModel _statsViewModel;
    private readonly CardManager _cardManager;
    private bool _initializingPricePicker;
    private bool _initializingPricePrefSwitches;

    public SettingsPage(StatsViewModel statsViewModel, CardManager cardManager)
    {
        InitializeComponent();
        _statsViewModel = statsViewModel;
        _cardManager = cardManager;

        _cardManager.OnProgress += (msg, pct) =>
        {
            if (DownloadProgress.IsVisible)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DownloadProgress.Progress = pct / 100.0;
                    DownloadStatusLabel.Text = msg;
                });
            }
        };

        _cardManager.OnDatabaseReady += () =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                DbStatusLabel.Text = "Connected";
                DbStatusLabel.TextColor = Color.FromArgb("#4CAF50");
                DownloadProgress.IsVisible = false;
                DownloadStatusLabel.Text = "Download complete!";
                CancelDownloadBtn.IsVisible = false;
                ForceDbUpdateBtn.IsVisible = true;
                _statsViewModel.InvalidateStats();
                await _statsViewModel.LoadStatsAsync();
            });
        };

        _cardManager.OnDatabaseError += _ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DbStatusLabel.Text = "Download failed";
                DbStatusLabel.TextColor = Color.FromArgb("#F44336");
                ForceDbUpdateBtn.IsVisible = true;
                DownloadProgress.IsVisible = false;
                DownloadStatusLabel.Text = "Download failed. Please try again.";
                CancelDownloadBtn.IsVisible = false;
            });
        };

        _cardManager.OnDownloadCancelled += () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DownloadProgress.IsVisible = false;
                CancelDownloadBtn.IsVisible = false;
                ForceDbUpdateBtn.IsVisible = true;
                DownloadStatusLabel.Text = "Download cancelled.";
            });
        };

        _statsViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(StatsViewModel.Storage) or nameof(StatsViewModel.CacheStats))
                MainThread.BeginInvokeOnMainThread(UpdateStorageUi);
        };

        InitPriceVendorPicker();
        InitPricePreferenceSwitches();
        UpdatePriceSectionVisibility();
    }

    private void InitPricePreferenceSwitches()
    {
        _initializingPricePrefSwitches = true;
        try
        {
            PricesDataSwitch.IsToggled = PricePreferences.PricesDataEnabled;
            CollectionPriceSwitch.IsToggled = PricePreferences.CollectionPriceDisplayEnabled;
            CollectionPriceSwitch.IsEnabled = PricePreferences.PricesDataEnabled;
        }
        finally
        {
            _initializingPricePrefSwitches = false;
        }
    }

    private void UpdatePriceSectionVisibility()
    {
        bool master = PricePreferences.PricesDataEnabled;
        PriceSourceSection.IsVisible = master;
        CollectionPriceSwitch.IsEnabled = master;
    }

    private async void OnPricesDataToggled(object? sender, ToggledEventArgs e)
    {
        if (_initializingPricePrefSwitches)
            return;

        PricePreferences.PricesDataEnabled = e.Value;
        if (!e.Value)
        {
            _initializingPricePrefSwitches = true;
            try
            {
                PricePreferences.CollectionPriceDisplayEnabled = false;
                CollectionPriceSwitch.IsToggled = false;
            }
            finally
            {
                _initializingPricePrefSwitches = false;
            }
            await _cardManager.DisablePricesSubsystemAsync();
        }
        else
        {
            try
            {
                await _cardManager.InitializePricesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogStuff($"Enable prices: {ex.Message}", LogLevel.Error);
            }
        }

        UpdatePriceSectionVisibility();
        PricePreferences.NotifyPriceDisplayPreferencesChanged();
        _statsViewModel.InvalidateStats();
        await _statsViewModel.LoadStatsAsync();
        MainThread.BeginInvokeOnMainThread(UpdateStorageUi);
    }

    private async void OnCollectionPriceToggled(object? sender, ToggledEventArgs e)
    {
        if (_initializingPricePrefSwitches)
            return;

        PricePreferences.CollectionPriceDisplayEnabled = e.Value;
        UpdatePriceSectionVisibility();
        PricePreferences.NotifyPriceDisplayPreferencesChanged();
        await _statsViewModel.RefreshTotalValueAsync();
    }

    private static readonly string[] PriceVendorNames = ["TCG Player", "Cardmarket", "Card Kingdom", "ManaPool"];
    private static readonly PriceVendor[] PriceVendorValues = [PriceVendor.TcgPlayer, PriceVendor.Cardmarket, PriceVendor.CardKingdom, PriceVendor.ManaPool];

    private void InitPriceVendorPicker()
    {
        _initializingPricePicker = true;
        try
        {
            PriceVendorPicker.ItemsSource = PriceVendorNames;
            var priority = PriceDisplayHelper.GetVendorPriority();
            var first = priority.Length > 0 ? priority[0] : PriceVendor.TcgPlayer;
            var idx = Array.IndexOf(PriceVendorValues, first);
            PriceVendorPicker.SelectedIndex = idx >= 0 ? idx : 0;
        }
        finally
        {
            _initializingPricePicker = false;
        }
    }

    private async void OnPriceVendorPickerChanged(object? sender, EventArgs e)
    {
        if (_initializingPricePicker || PriceVendorPicker.SelectedIndex < 0 || PriceVendorPicker.SelectedIndex >= PriceVendorValues.Length)
            return;
        PriceDisplayHelper.SetPreferredVendor(PriceVendorValues[PriceVendorPicker.SelectedIndex]);
        await _statsViewModel.RefreshTotalValueAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_cardManager.DatabaseManager.IsConnected)
        {
            DbStatusLabel.Text = "Connected";
            DbStatusLabel.TextColor = Color.FromArgb("#4CAF50");
        }
        else if (!AppDataManager.MtgDatabaseExists())
        {
            DbStatusLabel.Text = "Database not downloaded";
            DbStatusLabel.TextColor = Color.FromArgb("#F44336");
        }
        else
        {
            DbStatusLabel.Text = "Database exists but not connected";
            DbStatusLabel.TextColor = Color.FromArgb("#FFC107");
        }

        if (!DownloadProgress.IsVisible)
            ForceDbUpdateBtn.IsVisible = true;

        InitPricePreferenceSwitches();
        UpdatePriceSectionVisibility();

        if (_statsViewModel.IsStatsStale)
            await _statsViewModel.LoadStatsAsync();
        else
            MainThread.BeginInvokeOnMainThread(UpdateStorageUi);
    }

    private void UpdateStorageUi()
    {
        var storage = _statsViewModel.Storage;
        MtgDbSizeLabel.Text = FormatSize(storage.MtgDatabaseSize);
        CollectionDbSizeLabel.Text = FormatSize(storage.CollectionDatabaseSize);
        PricesDbSizeLabel.Text = FormatSize(storage.PricesDatabaseSize);
        ImageCacheSizeLabel.Text = FormatSize(storage.ImageCacheSize);
        TotalStorageSizeLabel.Text = FormatSize(storage.TotalSize);
        CacheStatsLabel.Text = _statsViewModel.CacheStats;
    }

    private static string FormatSize(long bytes) =>
        (bytes / (1024.0 * 1024.0)).ToString("F1") + " MB";

    private void OnDownloadDbClicked(object? sender, EventArgs e)
    {
        ForceDbUpdateBtn.IsVisible = false;
        DownloadProgress.IsVisible = true;
        DownloadStatusLabel.IsVisible = true;
        CancelDownloadBtn.IsVisible = true;

        _cardManager.DownloadDatabase();
    }

    private void OnCancelDownloadClicked(object? sender, EventArgs e) =>
        _cardManager.CancelDownload();

    private async void OnClearCacheClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(UserMessages.ClearCacheTitle, UserMessages.ClearCacheMessage, "Yes", "No");
        if (!confirm) return;

        _statsViewModel.ClearCacheCommand.Execute(null);
        await Task.Delay(500);
        CacheStatsLabel.Text = _statsViewModel.CacheStats;
        UpdateStorageUi();
    }

    private async void OnExportLogClicked(object? sender, EventArgs e)
    {
        try
        {
            Logger.LogStuff("User requested log export.", LogLevel.Info);
            await Task.Delay(350);

            var source = Logger.GetLogFilePath();
            if (!File.Exists(source))
            {
                await DisplayAlertAsync("Export log", "No log file exists yet. Use the app a bit and try again.", "OK");
                return;
            }

            var dest = Path.Combine(FileSystem.CacheDirectory, $"aethervault_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");
            File.Copy(source, dest, overwrite: true);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "AetherVault log",
                File = new ShareFile(dest)
            });
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"Log export failed: {ex.Message}", LogLevel.Error);
            await DisplayAlertAsync("Export log", $"Could not share the log: {ex.Message}", "OK");
        }
    }
}
