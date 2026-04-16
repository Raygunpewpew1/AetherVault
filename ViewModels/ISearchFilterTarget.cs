using AetherVault.Core;

namespace AetherVault.ViewModels;

/// <summary>
/// Target for the shared search filters sheet popup.
/// Implemented by <see cref="SearchViewModel"/>, <see cref="CardSearchPickerViewModel"/>, and <see cref="CollectionViewModel"/>.
/// </summary>
public interface ISearchFilterTarget
{
    string SearchText { get; set; }
    SearchOptions CurrentOptions { get; set; }
    Task ApplyFiltersAndSearchAsync(SearchOptions options);
}
