using CommunityToolkit.Maui.Views;

namespace AetherVault.Pages;

public partial class DeckAddCardsBrowsePopup : Popup
{
    public DeckAddCardsBrowsePopup()
    {
        InitializeComponent();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await CloseAsync();
}
