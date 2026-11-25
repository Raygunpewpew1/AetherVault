using AetherVault.Core;
using AetherVault.Services;
using AetherVault.Services.DeckBuilder;
using AetherVault.ViewModels;
using SkiaSharp.Views.Maui;

namespace AetherVault.Pages;

[QueryProperty(nameof(CardUuid), "uuid")]
public partial class CardDetailPage : ContentPage
{
    private readonly CardDetailViewModel _viewModel;
    private readonly DeckBuilderService _deckService;
    private readonly IServiceProvider _serviceProvider;
    private string _cardUuid = "";
    private bool _isSwipeAnimating;
    private bool _flipAnimating;

    public string CardUuid
    {
        get => _cardUuid;
        set
        {
            _cardUuid = value;
            _ = LoadCard();
        }
    }

    public CardDetailPage(CardDetailViewModel viewModel, DeckBuilderService deckService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _deckService = deckService;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        SwipeContainer.SwipedLeft += () => _ = HandleSwipeAsync(isNext: true);
        SwipeContainer.SwipedRight += () => _ = HandleSwipeAsync(isNext: false);
        FoilCard.Tapped += (_, _) => _ = OnFoilTappedAsync();

        SetSvgCache.SymbolsUpdated += OnSetSymbolsUpdated;
        Unloaded += (_, _) =>
        {
            SetSvgCache.SymbolsUpdated -= OnSetSymbolsUpdated;
            FoilCard.Dispose();
            _viewModel.Dispose();
        };
    }

    private void OnSetSymbolsUpdated() =>
        MainThread.BeginInvokeOnMainThread(() => SetSymbolView.InvalidateSurface());

    /// <summary>Back button returns to the deck editor via modal pop instead of Shell routes.</summary>
    public void PrepareAsDeckChildModal(Func<Task> dismissModalAsync)
    {
        _viewModel.BackNavigationAsync = dismissModalAsync;
    }

    protected override void OnDisappearing()
    {
        _viewModel.BackNavigationAsync = null;
        base.OnDisappearing();
    }

    private async Task LoadCard()
    {
        if (string.IsNullOrEmpty(_cardUuid)) return;

        await _viewModel.LoadCardAsync(_cardUuid);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            CardFlipHost.RotationY = 0;
            CardFlipHost.Scale = 1;
            FoilCard.ResetTilt();
            FoilCard.Invalidate();
        });

        if (_viewModel.ShowGalleryNavigation)
            ShowSwipeHintIfNeededAsync();

        if (_viewModel.HasMultipleFaces)
            await PlayFlipAvailableHighlightAsync();
    }

    private async void ShowSwipeHintIfNeededAsync()
    {
        if (Preferences.Default.Get("SwipeHintShown", false)) return;
        Preferences.Default.Set("SwipeHintShown", true);
        SwipeHintLabel.Opacity = 1;
        await SwipeHintLabel.FadeToAsync(0, 2500);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CardDetailViewModel.CardImage) or nameof(CardDetailViewModel.CurrentFace))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_viewModel.IsSetSymbolVisible)
                    SetSymbolView.InvalidateSurface();
                FoilCard.Invalidate();
            });
        }
    }

    private void OnSetSymbolPaint(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        var setCode = _viewModel.CurrentFace?.SetCode;
        if (string.IsNullOrEmpty(setCode)) return;

        var rarityColor = _viewModel.RarityColor.ToSKColor();
        SetSvgCache.DrawSymbol(canvas, setCode, e.Info.Rect, rarityColor);
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        int currentQty = await _viewModel.GetCollectionQuantityAsync();

        var page = _serviceProvider.GetRequiredService<CollectionAddPage>();
        page.CardName = _viewModel.Card.Name;
        page.SetInfo = $"{_viewModel.Card.SetCode} #{_viewModel.Card.Number}";
        page.CurrentQty = currentQty;
        await Navigation.PushModalAsync(page);
        var result = await page.WaitForResultAsync();

        if (result is CollectionAddResult r)
        {
            await _viewModel.AddToCollectionWithFinishAsync(r.NewQuantity, r.IsFoil, r.IsEtched);
            _viewModel.StatusIsError = false;
            _viewModel.StatusMessage = UserMessages.CardAddedToCollection(r.NewQuantity, _viewModel.Card.Name);
        }
    }

    private async void OnAddToDeckClicked(object? sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<AddToDeckPage>();
        page.CardUuid = _viewModel.Card.Uuid;
        page.CardName = _viewModel.Card.Name;
        await Navigation.PushModalAsync(page);
        var result = await page.WaitForResultAsync();

        if (result != null)
        {
            var validation = await _deckService.AddCardAsync(result.DeckId, _viewModel.Card.Uuid, result.Quantity, result.Section);
            if (validation.IsError)
            {
                _viewModel.StatusIsError = true;
                _viewModel.StatusMessage = validation.Message ?? UserMessages.CouldNotAddCardToDeck();
            }
            else
            {
                _viewModel.StatusIsError = false;
                _viewModel.StatusMessage = UserMessages.CardAddedToDeck(result.Quantity, _viewModel.Card.Name, result.DeckName);
            }
        }
    }

    private async void OnRemoveClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(UserMessages.RemoveTitle, UserMessages.RemoveFromCollectionMessage(_viewModel.Card.Name), "Yes", "No");
        if (confirm)
            _viewModel.RemoveFromCollectionCommand.Execute(null);
    }

    private async void OnFlipClicked(object? sender, EventArgs e)
        => await RunFlipFaceAnimationAsync();

    private async Task OnFoilTappedAsync()
    {
        if (_viewModel.HasMultipleFaces && !_flipAnimating)
            await RunFlipFaceAnimationAsync();
    }

    private void OnSwipedLeft(object? sender, SwipedEventArgs e)
        => _ = HandleSwipeAsync(isNext: true);

    private void OnSwipedRight(object? sender, SwipedEventArgs e)
        => _ = HandleSwipeAsync(isNext: false);

    private async Task HandleSwipeAsync(bool isNext)
    {
        if (_isSwipeAnimating)
            return;

        _isSwipeAnimating = true;

        try
        {
            if (!_viewModel.ShowGalleryNavigation || CardContentRoot is null)
            {
                if (isNext)
                    _viewModel.NavigateNextCardCommand.Execute(null);
                else
                    _viewModel.NavigatePreviousCardCommand.Execute(null);

                return;
            }

            var width = DetailScreen.Width;
            if (width <= 0)
                width = Width;

            if (width <= 0)
            {
                if (isNext)
                    _viewModel.NavigateNextCardCommand.Execute(null);
                else
                    _viewModel.NavigatePreviousCardCommand.Execute(null);

                return;
            }

            var direction = isNext ? -1 : 1;
            var travel = Math.Max(48, width * 0.12);
            const uint duration = 140;

            await CardContentRoot.TranslateToAsync(direction * travel, 0, duration, Easing.CubicOut);

            if (isNext)
                _viewModel.NavigateNextCardCommand.Execute(null);
            else
                _viewModel.NavigatePreviousCardCommand.Execute(null);

            FoilCard.ResetTilt();
            CardContentRoot.TranslationX = -direction * travel;
            await CardContentRoot.TranslateToAsync(0, 0, duration, Easing.CubicIn);
        }
        finally
        {
            _isSwipeAnimating = false;
        }
    }

    private async Task PlayFlipAvailableHighlightAsync()
    {
        if (!_viewModel.HasMultipleFaces) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!IsLoaded) return;

            var savedStroke = CardArtChrome.Stroke;
            var savedThick = CardArtChrome.StrokeThickness;

            CardArtChrome.Stroke = Palette.Primary;
            CardArtChrome.StrokeThickness = 2;

            await CardFlipHost.ScaleToAsync(1.04, 110);
            await CardFlipHost.ScaleToAsync(1, 220, Easing.SpringOut);

            CardArtChrome.Stroke = savedStroke;
            CardArtChrome.StrokeThickness = savedThick;
        });
    }

    private static Task AnimateRotationYAsync(VisualElement view, double from, double to, uint durationMs, Easing easing)
    {
        view.AbortAnimation("CardDetailFlipY");
        view.RotationY = from;
        var tcs = new TaskCompletionSource();
        new Animation(v => view.RotationY = v, from, to).Commit(view, "CardDetailFlipY", 16, durationMs, easing, (_, _) => tcs.TrySetResult());
        return tcs.Task;
    }

    private async Task RunFlipFaceAnimationAsync()
    {
        if (!_viewModel.HasMultipleFaces || _flipAnimating) return;

        _flipAnimating = true;
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                FoilCard.ResetTilt();
                const uint halfMs = 220;
                await AnimateRotationYAsync(CardFlipHost, CardFlipHost.RotationY, 90, halfMs, Easing.CubicIn);
                await _viewModel.AdvanceFaceAndLoadImageAsync();
                FoilCard.Invalidate();
                await AnimateRotationYAsync(CardFlipHost, 90, 0, halfMs, Easing.CubicOut);
            });
        }
        finally
        {
            _flipAnimating = false;
        }
    }
}
