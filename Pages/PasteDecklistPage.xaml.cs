namespace AetherVault.Pages;

public partial class PasteDecklistPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PasteDecklistPage()
    {
        InitializeComponent();
    }

    public Task<string?> WaitForTextAsync() => _tcs.Task;

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        var raw = DeckEditor.Text;
        if (string.IsNullOrWhiteSpace(raw))
        {
            ErrorLabel.Text = "Paste a deck list first.";
            ErrorLabel.IsVisible = true;
            return;
        }

        ErrorLabel.IsVisible = false;
        _tcs.TrySetResult(raw);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _tcs.TrySetResult(null);
    }
}
