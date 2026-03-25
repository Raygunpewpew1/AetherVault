namespace AetherVault.Services;

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

public interface IToastService
{
    void Show(string message, int durationMs = 3000);

    /// <summary>Shows a toast with an action button (e.g. "Undo"). Tapping the button invokes the action and dismisses the toast.</summary>
    void ShowWithAction(string message, string actionLabel, Action action, int durationMs = 5000);
}

/// <summary>
/// Shows toasts in a window-level overlay so page Content is never replaced (avoids focus/content loss and nested-toast bugs).
/// Call <see cref="SetOverlayHost"/> when the window root is created (e.g. in App.CreateWindow).
/// </summary>
public class ToastService : IToastService
{
    /// <summary>Grid used as the toast overlay; set by the app when the window root is built. Toasts are added as children and removed when dismissed.</summary>
    internal static Grid? OverlayHost { get; set; }

    /// <summary>Called when building the window root so toasts render in an overlay instead of replacing page Content.</summary>
    public static void SetOverlayHost(Grid overlayGrid)
    {
        OverlayHost = overlayGrid;
    }

    public void Show(string message, int durationMs = 3000)
    {
        MainThread.BeginInvokeOnMainThread(() => _ = ShowBannerAsync(message, durationMs));
    }

    public void ShowWithAction(string message, string actionLabel, Action action, int durationMs = 5000)
    {
        MainThread.BeginInvokeOnMainThread(() => _ = ShowBannerWithActionAsync(message, actionLabel, action, durationMs));
    }

    private static Grid GetOverlay()
    {
        var overlay = OverlayHost;
        if (overlay != null)
            return overlay;
        throw new InvalidOperationException("ToastService overlay not set. Ensure the window root is built with ToastService.SetOverlayHost(overlayGrid).");
    }

    /// <summary>
    /// Snackbar without an anchor often does not appear on Android when no toast overlay Grid is set.
    /// Anchor to the visible page so Material snackbar attaches to the activity.
    /// </summary>
    private static IView? GetSnackbarAnchor()
    {
        try
        {
            if (Shell.Current?.CurrentPage is IView shellPage)
                return shellPage;
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is IView windowPage)
                return windowPage;
        }
        catch
        {
            // Shell/window not ready
        }

        return null;
    }

    private static async Task ShowBannerWithActionAsync(string message, string actionLabel, Action action, int durationMs)
    {
        if (OverlayHost == null)
        {
            var duration = TimeSpan.FromMilliseconds(Math.Max(durationMs, 2000));
            var anchor = GetSnackbarAnchor();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Extension anchors the native snackbar to this element (required on Android without overlay).
                if (anchor is VisualElement ve)
                {
                    await ve.DisplaySnackbar(message, action, actionLabel, duration);
                    return;
                }

                var snackbar = Snackbar.Make(message, action, actionLabel, duration, null, anchor);
                await snackbar.Show();
            });
            return;
        }

        var overlay = OverlayHost;
        var dismissed = false;

        var actionButton = new Button
        {
            Text = actionLabel,
            FontSize = 14,
            HeightRequest = 36,
            Padding = new Thickness(16, 0),
            BackgroundColor = Colors.White,
            TextColor = Colors.Black,
            CornerRadius = 18,
        };

        var layout = new HorizontalStackLayout
        {
            Spacing = 12,
            VerticalOptions = LayoutOptions.Center,
        };
        layout.Children.Add(new Label
        {
            Text = message,
            TextColor = Colors.White,
            FontSize = 14,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            MaximumWidthRequest = 220,
            LineBreakMode = LineBreakMode.TailTruncation,
        });
        layout.Children.Add(actionButton);

        var banner = new Border
        {
            BackgroundColor = Color.FromArgb("#DD1E1E1E"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(16, 10),
            Margin = new Thickness(16, 8, 16, 0),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Start,
            ZIndex = 999,
            Opacity = 0,
            Content = layout,
        };

        void RemoveBanner()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (overlay.Children.Contains(banner))
                    overlay.Children.Remove(banner);
            });
        }

        actionButton.Clicked += (_, _) =>
        {
            if (dismissed) return;
            dismissed = true;
            RemoveBanner();
            MainThread.BeginInvokeOnMainThread(action);
        };

        MainThread.BeginInvokeOnMainThread(() => overlay.Children.Add(banner));

        try
        {
            await banner.FadeToAsync(1, 150);
            var delayMs = Math.Max(durationMs - 300, 200);
            while (delayMs > 0 && !dismissed)
            {
                await Task.Delay(Math.Min(100, delayMs));
                delayMs -= 100;
            }
            if (!dismissed)
                await banner.FadeToAsync(0, 150);
        }
        finally
        {
            if (!dismissed)
                RemoveBanner();
        }
    }

    private static async Task ShowBannerAsync(string message, int durationMs)
    {
        if (OverlayHost == null)
        {
            var duration = durationMs > 3000 ? ToastDuration.Long : ToastDuration.Short;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var toast = Toast.Make(message, duration);
                await toast.Show();
            });
            return;
        }

        var overlay = OverlayHost;
        var banner = new Border
        {
            BackgroundColor = Color.FromArgb("#DD1E1E1E"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(16, 10),
            Margin = new Thickness(16, 8, 16, 0),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Start,
            ZIndex = 999,
            Opacity = 0,
            Content = new Label
            {
                Text = message,
                TextColor = Colors.White,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
            }
        };

        MainThread.BeginInvokeOnMainThread(() => overlay.Children.Add(banner));

        try
        {
            await banner.FadeToAsync(1, 150);
            await Task.Delay(Math.Max(durationMs - 300, 200));
            await banner.FadeToAsync(0, 150);
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (overlay.Children.Contains(banner))
                    overlay.Children.Remove(banner);
            });
        }
    }
}
