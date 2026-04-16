namespace AetherVault.Core;

/// <summary>
/// Centralizes navigation to <c>deckdetail</c> so debug sessions (Android debugger + XAML Hot Reload) can
/// avoid re-entrant Shell transitions from <see cref="CollectionView"/> selection handlers, which otherwise
/// may never complete <see cref="Shell.GoToAsync(ShellNavigationState, bool)"/> while the selection halo remains visible.
/// </summary>
public static class ShellDeckDetailNavigation
{
    public static async Task GoToAsync(int deckId)
    {
        // Let the current input / selection pipeline finish before pushing the route.
        await Task.Yield();
#if DEBUG
        await Shell.Current.GoToAsync($"deckdetail?deckId={deckId}", animate: false);
#else
        await Shell.Current.GoToAsync($"deckdetail?deckId={deckId}");
#endif
    }
}
