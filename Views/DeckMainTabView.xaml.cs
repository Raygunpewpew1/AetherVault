using AetherVault.ViewModels;

namespace AetherVault.Views;

public partial class DeckMainTabView : ContentView
{
    public DeckMainTabView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Row tap opens quick detail via CollectionView SelectionChanged (avoids gesture fights on Android; see AGENTS.md).
    /// List rows use ⋯ on the thumbnail for move/remove (same sheet as grid); no SwipeView.
    /// </summary>
    private void OnMainDeckItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not CollectionView cv) return;
        if (e.CurrentSelection.FirstOrDefault() is not DeckCardDisplayItem item) return;
        cv.SelectedItem = null;
        if (BindingContext is DeckDetailViewModel vm)
            vm.DeckListItemTappedCommand.Execute(item);
    }
}
