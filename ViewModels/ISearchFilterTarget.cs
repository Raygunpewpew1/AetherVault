using AetherVault.Core;

namespace AetherVault.ViewModels;

/// <summary>
/// Target for the shared search filters sheet popup.
/// Implemented by SearchViewModel and CardSearchPickerViewModel.
/// </summary>
public interface ISearchFilterTarget
{
    string SearchText { get; }
    SearchOptions CurrentOptions { get; set; }
    Task ApplyFiltersAndSearchAsync(SearchOptions options);
}
