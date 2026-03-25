using AetherVault.ViewModels;

namespace AetherVault.Views;

public partial class DeckSideboardTabView : ContentView
{
    public DeckSideboardTabView()
    {
        InitializeComponent();
    }

    private void OnSideboardItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not CollectionView cv) return;
        if (e.CurrentSelection.FirstOrDefault() is not DeckCardDisplayItem item) return;
        cv.SelectedItem = null;
        if (BindingContext is DeckDetailViewModel vm)
            vm.DeckListItemTappedCommand.Execute(item);
    }
}
