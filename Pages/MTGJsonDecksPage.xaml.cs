using AetherVault.Models;
using AetherVault.Services;
using AetherVault.ViewModels;
#if ANDROID
using Android.OS;
#endif

namespace AetherVault.Pages;

public partial class MtgJsonDecksPage : ContentPage
{
    private readonly MtgJsonDecksViewModel _viewModel;

    public MtgJsonDecksPage(MtgJsonDecksViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        var apiLevel = (int)Build.VERSION.SdkInt;
#else
        const int apiLevel = 0;
#endif
        Logger.LogStuff(
            $"[MtgJsonDecks] Page appear manufacturer={DeviceInfo.Manufacturer} model={DeviceInfo.Model} " +
            $"version={DeviceInfo.VersionString} idiom={DeviceInfo.Idiom} apiLevel={apiLevel} logFile={AppDataManager.GetLogPath()}",
            LogLevel.Info);

        if (_viewModel.Decks.Count == 0 && !_viewModel.IsBusy)
            await _viewModel.LoadDeckListAsync(false);
    }

    private async void OnDeckSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is MtgJsonDeckListEntry entry)
        {
            Logger.LogStuff($"[MtgJsonDecks] Row selected file={entry.FileName}", LogLevel.Info);
            if (sender is CollectionView cv)
                cv.SelectedItem = null;
            await _viewModel.ImportDeckAsync(entry);
        }
    }
}
