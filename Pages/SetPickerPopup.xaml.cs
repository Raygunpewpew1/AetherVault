using AetherVault.Core;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;

namespace AetherVault.Pages;

public partial class SetPickerPopup : Popup
{
    private const int SearchDebounceMs = 250;

    private readonly IReadOnlyList<SetInfo> _allSets;
    private readonly Action<int> _onSelected;
    private readonly Dictionary<string, int> _codeToIndex;
    private readonly ObservableCollection<SetInfo> _filteredSets = [];
    private CancellationTokenSource? _searchDebounceCts;
    private int _filterGeneration;

    public SetPickerPopup(IReadOnlyList<SetInfo> sets, int initialIndex, Action<int> onSelected)
    {
        InitializeComponent();
        _allSets = sets;
        _onSelected = onSelected;
        _codeToIndex = BuildCodeIndex(sets);

        SetsList.ItemsSource = _filteredSets;
        _ = LoadInitialListAsync();

        SearchEntry.TextChanged += OnSearchTextChanged;
    }

    private static Dictionary<string, int> BuildCodeIndex(IReadOnlyList<SetInfo> sets)
    {
        var map = new Dictionary<string, int>(sets.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < sets.Count; i++)
        {
            var code = sets[i].Code;
            if (!string.IsNullOrEmpty(code))
                map.TryAdd(code, i);
        }

        return map;
    }

    private async Task LoadInitialListAsync()
    {
        var all = await Task.Run(() => new List<SetInfo>(_allSets)).ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _filteredSets.Clear();
            foreach (var s in all)
                _filteredSets.Add(s);
        });
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;
        var text = e.NewTextValue ?? "";
        var gen = Interlocked.Increment(ref _filterGeneration);
        _ = ApplyDebouncedFilterAsync(text, token, gen);
    }

    private async Task ApplyDebouncedFilterAsync(string search, CancellationToken debounceToken, int generation)
    {
        try
        {
            await Task.Delay(SearchDebounceMs, debounceToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (generation != Volatile.Read(ref _filterGeneration))
            return;

        var q = search.Trim();
        var filtered = await Task.Run(() => FilterSets(q)).ConfigureAwait(false);

        if (generation != Volatile.Read(ref _filterGeneration))
            return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (generation != Volatile.Read(ref _filterGeneration))
                return;
            _filteredSets.Clear();
            foreach (var s in filtered)
                _filteredSets.Add(s);
        });
    }

    private List<SetInfo> FilterSets(string q)
    {
        if (string.IsNullOrEmpty(q))
            return [.. _allSets];

        var result = new List<SetInfo>();
        foreach (var s in _allSets)
        {
            if (s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.Code.Contains(q, StringComparison.OrdinalIgnoreCase))
                result.Add(s);
        }

        return result;
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0) return;

        if (e.CurrentSelection[0] is SetInfo selected)
        {
            if (_codeToIndex.TryGetValue(selected.Code, out var idx))
            {
                _onSelected(idx);
                await CloseAsync();
            }
        }
    }

}
