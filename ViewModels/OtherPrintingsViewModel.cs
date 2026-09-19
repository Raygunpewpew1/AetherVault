using AetherVault.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AetherVault.ViewModels;

public partial class OtherPrintingsViewModel : ObservableObject
{
    private IReadOnlyList<OtherPrintingSummary> _all = [];

    public event Action<string?>? PrintingSelected;

    public ObservableCollection<OtherPrintingSummary> FilteredPrintings { get; } = [];

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = "";

    public int TotalCount => _all.Count;

    public string HeaderSubtitle =>
        TotalCount == 1 ? "1 printing" : $"{TotalCount} printings";

    public void Configure(IReadOnlyList<OtherPrintingSummary> printings)
    {
        _all = printings;
        SearchQuery = "";
        ApplyFilter();
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = SearchQuery.Trim();
        IEnumerable<OtherPrintingSummary> rows = _all;
        if (!string.IsNullOrEmpty(query))
        {
            var needle = query.ToLowerInvariant();
            rows = _all.Where(p => p.SearchHaystack.Contains(needle, StringComparison.Ordinal));
        }

        FilteredPrintings.Clear();
        foreach (var row in rows)
            FilteredPrintings.Add(row);
    }

    [RelayCommand]
    private void SelectPrinting(OtherPrintingSummary? printing)
    {
        if (printing is null || string.IsNullOrEmpty(printing.Uuid))
            return;

        PrintingSelected?.Invoke(printing.Uuid);
    }

    [RelayCommand]
    private void Cancel() => PrintingSelected?.Invoke(null);
}
