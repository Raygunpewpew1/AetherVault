using AetherVault.ViewModels;

namespace AetherVault.Views;

public partial class DeckMainTabView : ContentView
{
    public DeckMainTabView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// SwipeView + tap on the same row fights for touches on Android; SelectionChanged avoids that (see AGENTS.md).
    /// Shared by standard, compact, and grid <see cref="CollectionView"/>s.
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
