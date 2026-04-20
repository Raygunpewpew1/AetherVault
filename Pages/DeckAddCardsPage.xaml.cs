using AetherVault.Controls;
using AetherVault.ViewModels;

namespace AetherVault.Pages;

public partial class DeckAddCardsPage : ContentPage
{
    private readonly DeckAddCardsViewModel _addCardsVm;
    private Func<Task>? _dismissModal;

    public DeckAddCardsPage(DeckAddCardsViewModel addCardsVm)
    {
        InitializeComponent();
        _addCardsVm = addCardsVm;
        AddResultsCardGrid.CardClicked += OnAddResultCardClicked;
    }

    private void OnAddResultCardClicked(string cardUuid) => _addCardsVm.OnResultCardClicked(cardUuid);

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _addCardsVm.AttachGrid(AddResultsCardGrid);
        AddResultsCardGrid.OnResume();
        _addCardsVm.NotifyAddCardsSheetAppeared();
    }

    /// <summary>Pops the modal using the same navigation object that opened it.</summary>
    public void Init(DeckDetailViewModel deckVm, Func<Task> dismissModal)
    {
        var pending = deckVm.ConsumePendingAddModalQuickList();
        _addCardsVm.PrepareModalTargetFromDeckTab(deckVm.SelectedSectionIndex);
        _addCardsVm.AttachHost(deckVm);
        if (pending != null)
            _addCardsVm.ApplyQuickBrowseListFromPending(pending);

        BindingContext = _addCardsVm;
        _dismissModal = dismissModal;
        _addCardsVm.AddCardsModalDismissAction = async () =>
        {
            _addCardsVm.ClearAddCardSearch();
            await dismissModal();
        };
    }

    protected override void OnDisappearing()
    {
        _addCardsVm.AddCardsModalDismissAction = null;
        _addCardsVm.DetachHost();
        _addCardsVm.DetachGrid();
        _addCardsVm.ClearAddCardSearch();
        base.OnDisappearing();
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        _addCardsVm.ClearAddCardSearch();

        if (_dismissModal != null)
            await _dismissModal();
        else
            await Navigation.PopModalAsync();
    }
}
