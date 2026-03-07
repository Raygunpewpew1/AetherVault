using AetherVault.ViewModels;

namespace AetherVault.Pages;

public partial class CardQuickDetailPage : ContentPage
{
    /// <summary>Set by caller before pushing this page as modal.</summary>
    public DeckCardDisplayItem? DisplayItem { get; set; }

    public CardQuickDetailPage()
    {
        InitializeComponent();
        ToolbarItems.Add(new ToolbarItem("Close", null, async () => await Navigation.PopModalAsync()));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (DisplayItem != null)
            BindingContext = DisplayItem;
    }

    private async void OnViewFullClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(DisplayItem?.CardUuid)) return;
        await Navigation.PopModalAsync();
        await Shell.Current.GoToAsync($"carddetail?uuid={Uri.EscapeDataString(DisplayItem.CardUuid)}");
    }
}
