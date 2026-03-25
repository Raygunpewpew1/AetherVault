using AetherVault.Models;
using AetherVault.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AetherVault.ViewModels;

public partial class MtgJsonDecksViewModel : BaseViewModel
{
    private const int SearchFilterDebounceMs = 250;
    private const string DiagTag = "[MtgJsonDecks]";

    private readonly MtgJsonDeckListService _deckListService;
    private readonly MtgJsonDeckImporter _importer;
    private readonly IToastService _toast;

    private CancellationTokenSource? _filterCts;
    private int _deckCatalogGeneration;
    private bool _suppressFilterCallbacks;

    [ObservableProperty]
    private ObservableCollection<MtgJsonDeckListEntry> _decks = [];

    [ObservableProperty]
    private ObservableCollection<MtgJsonDeckListEntry> _filteredDecks = [];

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedDeckType = "All";

    [ObservableProperty]
    private List<string> _availableDeckTypes = ["All"];

    [ObservableProperty]
    private MtgJsonDeckListEntry? _selectedDeck;

    private List<MtgJsonDeckListEntry> _allDecks = [];

    public MtgJsonDecksViewModel(MtgJsonDeckListService deckListService, MtgJsonDeckImporter importer, IToastService toast)
    {
        _deckListService = deckListService;
        _importer = importer;
        _toast = toast;
    }

    private static void LogDiag(string message) =>
        Logger.LogStuff($"{DiagTag} {message}", LogLevel.Info);

    [RelayCommand]
    public async Task Refresh()
    {
        await LoadDeckListAsync(true);
    }

    [RelayCommand]
    public async Task LoadDeckListAsync(bool forceRefresh = false)
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusIsError = false;
        StatusMessage = UserMessages.LoadingDeckList;

        var loadSw = Stopwatch.StartNew();
        LogDiag($"LoadDeckList start forceRefresh={forceRefresh}");

        try
        {
            var list = await _deckListService.GetDeckListAsync(forceRefresh);
            _allDecks = [.. list];
            var types = _allDecks.Select(d => d.Type ?? "").Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToList();
            _deckCatalogGeneration++;

            LogDiag($"LoadDeckList catalog rows={_allDecks.Count} distinctTypes={types.Count} afterFetchMs={loadSw.ElapsedMilliseconds}");

            _suppressFilterCallbacks = true;
            try
            {
                var uiSw = Stopwatch.StartNew();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AvailableDeckTypes = ["All", .. types];
                    if (!AvailableDeckTypes.Contains(SelectedDeckType))
                        SelectedDeckType = "All";
                    Decks = new ObservableCollection<MtgJsonDeckListEntry>(_allDecks);
                    StatusMessage = _allDecks.Count == 0 ? "No decks in catalog." : $"{(uint)_allDecks.Count} decks";
                });
                LogDiag($"LoadDeckList mainThreadUiMs={uiSw.ElapsedMilliseconds}");
            }
            finally
            {
                _suppressFilterCallbacks = false;
            }

            // Initial catalog bind: filter off the UI thread, then apply once (avoids empty list flash).
            string typeSnap = SelectedDeckType;
            string textSnap = SearchText;
            int gen = _deckCatalogGeneration;
            var cpuSw = Stopwatch.StartNew();
            var filtered = await Task.Run(() => FilterDecks(_allDecks, typeSnap, textSnap)).ConfigureAwait(false);
            LogDiag($"LoadDeckList initialFilterComputeMs={cpuSw.ElapsedMilliseconds} filteredCount={filtered.Count}");

            var applySw = Stopwatch.StartNew();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (gen != _deckCatalogGeneration) return;
                if (typeSnap != SelectedDeckType || textSnap != SearchText) return;
                FilteredDecks = new ObservableCollection<MtgJsonDeckListEntry>(filtered);
            });
            LogDiag($"LoadDeckList applyFilteredMs={applySw.ElapsedMilliseconds} totalMs={loadSw.ElapsedMilliseconds}");
        }
        catch (Exception ex)
        {
            LogDiag($"LoadDeckList failed: {ex.GetType().Name}: {ex.Message}");
            Logger.LogStuff(ex.StackTrace ?? "", LogLevel.Error);
            StatusIsError = true;
            StatusMessage = UserMessages.LoadFailed(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static List<MtgJsonDeckListEntry> FilterDecks(
        IReadOnlyList<MtgJsonDeckListEntry> allDecks,
        string selectedDeckType,
        string searchText)
    {
        IEnumerable<MtgJsonDeckListEntry> source = allDecks;
        if (selectedDeckType != "All")
            source = source.Where(d => string.Equals(d.Type, selectedDeckType, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var q = searchText.Trim();
            source = source.Where(d =>
                d.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (d.Type?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return source.ToList();
    }

    private async Task RunFilterAndApplyAsync(bool immediate, CancellationToken ct)
    {
        var totalSw = Stopwatch.StartNew();
        LogDiag($"Filter pipeline start immediate={immediate}");

        _filterCts?.Cancel();
        _filterCts = null;

        CancellationToken filterToken = ct;
        if (!immediate)
        {
            var linked = new CancellationTokenSource();
            _filterCts = linked;
            filterToken = linked.Token;
            try
            {
                await Task.Delay(SearchFilterDebounceMs, filterToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Logger.LogStuff($"{DiagTag} Filter debounce canceled (superseded)", LogLevel.Debug);
                return;
            }
        }

        LogDiag($"Filter after wait phaseMs={totalSw.ElapsedMilliseconds}");

        string type = "";
        string text = "";
        int generation = 0;
        List<MtgJsonDeckListEntry> snapshot = [];
        var snapSw = Stopwatch.StartNew();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            type = SelectedDeckType;
            text = SearchText;
            generation = _deckCatalogGeneration;
            snapshot = [.. _allDecks];
        });
        LogDiag($"Filter snapshot mainThreadMs={snapSw.ElapsedMilliseconds} gen={generation} snapshotCount={snapshot.Count} type={type} searchLen={text.Length}");

        List<MtgJsonDeckListEntry> filtered;
        try
        {
            var cpuSw = Stopwatch.StartNew();
            filtered = await Task.Run(() => FilterDecks(snapshot, type, text), filterToken).ConfigureAwait(false);
            LogDiag($"Filter computeMs={cpuSw.ElapsedMilliseconds} resultCount={filtered.Count}");
        }
        catch (OperationCanceledException)
        {
            Logger.LogStuff($"{DiagTag} Filter compute canceled", LogLevel.Debug);
            return;
        }

        var applySw = Stopwatch.StartNew();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (filterToken.IsCancellationRequested) return;
            if (generation != _deckCatalogGeneration) return;
            if (!string.Equals(type, SelectedDeckType, StringComparison.Ordinal) ||
                !string.Equals(text, SearchText, StringComparison.Ordinal))
            {
                LogDiag("Filter apply skipped (stale type/text/gen)");
                return;
            }

            FilteredDecks = new ObservableCollection<MtgJsonDeckListEntry>(filtered);
        });
        LogDiag($"Filter apply mainThreadMs={applySw.ElapsedMilliseconds} totalMs={totalSw.ElapsedMilliseconds}");
    }

    private void QueueFilter(bool immediate)
    {
        _ = FilterWithFaultLogAsync(immediate);
    }

    private async Task FilterWithFaultLogAsync(bool immediate)
    {
        try
        {
            await RunFilterAndApplyAsync(immediate, CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogDiag($"Filter pipeline fault immediate={immediate}: {ex.GetType().Name}: {ex.Message}");
            Logger.LogStuff(ex.StackTrace ?? "", LogLevel.Error);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_suppressFilterCallbacks) return;
        LogDiag($"SearchText changed len={value.Length}");
        QueueFilter(immediate: false);
    }

    partial void OnSelectedDeckTypeChanged(string value)
    {
        if (_suppressFilterCallbacks) return;
        LogDiag($"SelectedDeckType changed to {value}");
        QueueFilter(immediate: true);
    }

    [RelayCommand]
    public async Task ImportDeckAsync(MtgJsonDeckListEntry? entry)
    {
        var deckEntry = entry ?? SelectedDeck;
        if (deckEntry == null)
        {
            _toast?.Show(UserMessages.PleaseSelectDeck);
            return;
        }
        if (IsBusy) return;
        IsBusy = true;
        StatusIsError = false;
        StatusMessage = UserMessages.ImportingMtgJsonDeck;

        try
        {
            var deck = await _deckListService.GetDeckAsync(deckEntry.FileName);
            if (deck == null)
            {
                StatusIsError = true;
                StatusMessage = UserMessages.MtgJsonDeckImportFailed;
                _toast?.Show(UserMessages.MtgJsonDeckImportFailed);
                return;
            }

            var progress = new Progress<string>(msg => MainThread.BeginInvokeOnMainThread(() => StatusMessage = msg));
            var result = await _importer.ImportDeckAsync(deck, progress);

            if (!result.Success)
            {
                StatusIsError = true;
                StatusMessage = UserMessages.MtgJsonDeckImportFailed;
                _toast?.Show(UserMessages.MtgJsonDeckImportFailed);
                return;
            }

            StatusMessage = UserMessages.StatusClear;
            _toast?.Show(UserMessages.MtgJsonDeckImportedToast(deck.Name, result.CardsAdded));
            if (result.MissingUuids.Count > 0)
                Logger.LogStuff($"MTGJSON import: {result.MissingUuids.Count} UUIDs not in local DB.", LogLevel.Warning);

            await Shell.Current.GoToAsync($"deckdetail?deckId={result.DeckId}");
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"MTGJSON deck import failed: {ex.Message}", LogLevel.Error);
            StatusIsError = true;
            StatusMessage = UserMessages.ImportFailed(ex.Message);
            _toast?.Show(UserMessages.MtgJsonDeckImportFailed);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
