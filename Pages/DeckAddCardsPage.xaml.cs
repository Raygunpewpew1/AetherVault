using AetherVault.ViewModels;

namespace AetherVault.Pages;

public partial class DeckAddCardsPage : ContentPage
{
    private const double ChromeMaxHeightFraction = 0.42;
    private double _lastChromeCap;

    private Func<Task>? _dismissModal;

    public DeckAddCardsPage()
    {
        InitializeComponent();
        PageRootGrid.SizeChanged += OnPageRootGridSizeChanged;
    }

    /// <summary>
    /// ScrollView inside a proportional Grid row can expand to full content height on Android,
    /// starving the results row. Cap chrome height so the bottom list always gets space.
    /// </summary>
    private void OnPageRootGridSizeChanged(object? sender, EventArgs e) => ApplyChromeMaxHeight();

    private void ApplyChromeMaxHeight()
    {
        if (PageRootGrid.Height <= 1)
            return;
        double cap = PageRootGrid.Height * ChromeMaxHeightFraction;
        if (Math.Abs(cap - _lastChromeCap) < 0.5)
            return;
        _lastChromeCap = cap;
        ChromeScrollView.MaximumHeightRequest = cap;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyChromeMaxHeight();
    }

    /// <summary>Pops the modal using the same navigation object that opened it.</summary>
    public void Init(DeckDetailViewModel viewModel, Func<Task> dismissModal)
    {
        viewModel.PrepareAddCardsModal();
        BindingContext = viewModel;
        _dismissModal = dismissModal;
        viewModel.AddCardsModalDismissAction = async () =>
        {
            viewModel.ClearAddCardSearch();
            await dismissModal();
        };
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is DeckDetailViewModel vm)
        {
            vm.AddCardsModalDismissAction = null;
            vm.ClearAddCardSearch();
        }
        base.OnDisappearing();
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        if (BindingContext is DeckDetailViewModel vm)
            vm.ClearAddCardSearch();

        if (_dismissModal != null)
            await _dismissModal();
        else
            await Navigation.PopModalAsync();
    }
}
