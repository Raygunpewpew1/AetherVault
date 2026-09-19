using AetherVault.Models;
using AetherVault.ViewModels;

namespace AetherVault.Pages;

public partial class OtherPrintingsPage : ContentPage
{
    private readonly OtherPrintingsViewModel _viewModel;
    private TaskCompletionSource<string?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public OtherPrintingsPage(OtherPrintingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void Init(IReadOnlyList<OtherPrintingSummary> printings)
    {
        _tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _viewModel.PrintingSelected += OnPrintingSelected;
        _viewModel.Configure(printings);
    }

    public Task<string?> WaitForResultAsync() => _tcs.Task;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PrintingSelected -= OnPrintingSelected;
        if (!_tcs.Task.IsCompleted)
            _tcs.TrySetResult(null);
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.PrintingSelected -= OnPrintingSelected;
        if (!_tcs.Task.IsCompleted)
            _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private async void OnPrintingSelected(string? uuid)
    {
        _viewModel.PrintingSelected -= OnPrintingSelected;
        await Navigation.PopModalAsync();
        if (!_tcs.Task.IsCompleted)
            _tcs.TrySetResult(uuid);
    }
}
