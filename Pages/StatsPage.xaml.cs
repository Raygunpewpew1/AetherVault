using AetherVault.Services;
using AetherVault.ViewModels;

namespace AetherVault.Pages;

public partial class StatsPage : ContentPage
{
    private readonly StatsViewModel _viewModel;

    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(StatsViewModel.Stats))
                MainThread.BeginInvokeOnMainThread(UpdateCollectionStatsUi);
        };

        PricePreferences.PriceDisplayPreferencesChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(UpdatePriceRowVisibility);
    }

    private void UpdatePriceRowVisibility()
    {
        bool coll = PricePreferences.PricesDataEnabled && PricePreferences.CollectionPriceDisplayEnabled;
        TotalValueCaptionLabel.IsVisible = coll;
        TotalValueLabel.IsVisible = coll;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        UpdatePriceRowVisibility();

        if (_viewModel.IsStatsStale)
            await _viewModel.LoadStatsAsync();

        UpdateCollectionStatsUi();
    }

    private void UpdateCollectionStatsUi()
    {
        var stats = _viewModel.Stats;
        TotalCardsLabel.Text = stats.TotalCards.ToString();
        UniqueCardsLabel.Text = stats.UniqueCards.ToString();
        TotalValueLabel.Text = stats.TotalValue > 0 ? $"${stats.TotalValue:F2}" : "—";
        AvgCMCLabel.Text = stats.AvgCmc.ToString("F2");
        FoilCountLabel.Text = stats.FoilCount.ToString();

        CreatureCountLabel.Text = stats.CreatureCount.ToString();
        SpellCountLabel.Text = stats.SpellCount.ToString();
        LandCountLabel.Text = stats.LandCount.ToString();

        CommonLabel.Text = stats.CommonCount.ToString();
        UncommonLabel.Text = stats.UncommonCount.ToString();
        RareLabel.Text = stats.RareCount.ToString();
        MythicLabel.Text = stats.MythicCount.ToString();
    }
}
