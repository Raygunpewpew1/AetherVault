using AetherVault.Models;
using AetherVault.ViewModels;

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
        if (_viewModel.Decks.Count == 0 && !_viewModel.IsBusy)
            await _viewModel.LoadDeckListAsync(false);
    }

    private async void OnDeckSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is MtgJsonDeckListEntry entry)
        {
            if (sender is CollectionView cv)
                cv.SelectedItem = null;
            await _viewModel.ImportDeckAsync(entry);
        }
    }
}
