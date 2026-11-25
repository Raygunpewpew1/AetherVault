using AetherVault.Core;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace AetherVault.Controls;

/// <summary>
/// Premium card face: GPU Skia art, touch-driven tilt, foil shimmer, and light parallax.
/// Used only on Card Detail — keep collection grids flat/fast.
/// </summary>
public sealed class FoilCardView : ContentView, IDisposable
{
    public static readonly BindableProperty CardImageProperty = BindableProperty.Create(
        nameof(CardImage), typeof(SKImage), typeof(FoilCardView), null,
        propertyChanged: (b, _, _) => ((FoilCardView)b)._canvas.InvalidateSurface());

    public static readonly BindableProperty IsFoilEnabledProperty = BindableProperty.Create(
        nameof(IsFoilEnabled), typeof(bool), typeof(FoilCardView), true);

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(float), typeof(FoilCardView), 12f,
        propertyChanged: (b, _, _) => ((FoilCardView)b)._canvas.InvalidateSurface());

    private readonly SKGLView _canvas;
    private readonly SKPaint _imagePaint = new() { IsAntialias = true };
    private readonly SKPaint _shadowPaint = new()
    {
        IsAntialias = true,
        Color = new SKColor(0, 0, 0, 90),
        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 12)
    };
    private readonly SKPaint _foilPaint = new() { IsAntialias = true, BlendMode = SKBlendMode.SoftLight };
    private readonly SKPaint _specularPaint = new() { IsAntialias = true, BlendMode = SKBlendMode.Plus };

    private float _tiltX;
    private float _tiltY;
    private float _lightX = 0.5f;
    private float _lightY = 0.35f;
    private bool _dragging;
    private bool _disposed;
    private DateTime _lastInvalidateUtc = DateTime.MinValue;

    /// <summary>Raised when the user taps (short press) the card face — used for multi-face flip.</summary>
    public event EventHandler? Tapped;

    public SKImage? CardImage
    {
        get => (SKImage?)GetValue(CardImageProperty);
        set => SetValue(CardImageProperty, value);
    }

    public bool IsFoilEnabled
    {
        get => (bool)GetValue(IsFoilEnabledProperty);
        set => SetValue(IsFoilEnabledProperty, value);
    }

    public float CornerRadius
    {
        get => (float)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public FoilCardView()
    {
        _canvas = new SKGLView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            EnableTouchEvents = true,
            IgnorePixelScaling = false
        };
        _canvas.PaintSurface += OnPaintSurface;
        _canvas.Touch += OnTouch;

        Content = _canvas;
        BackgroundColor = Colors.Transparent;
    }

    public void Invalidate() => _canvas.InvalidateSurface();

    public void ResetTilt()
    {
        _tiltX = 0;
        _tiltY = 0;
        _lightX = 0.5f;
        _lightY = 0.35f;
        RotationX = 0;
        RotationY = 0;
        TranslationX = 0;
        TranslationY = 0;
        _canvas.InvalidateSurface();
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        var w = Math.Max(_canvas.CanvasSize.Width, 1);
        var h = Math.Max(_canvas.CanvasSize.Height, 1);

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _dragging = true;
                UpdateLight(e.Location.X / w, e.Location.Y / h);
                e.Handled = true;
                break;

            case SKTouchAction.Moved when _dragging:
                UpdateLight(e.Location.X / w, e.Location.Y / h);
                ApplyTiltFromLight();
                ThrottledInvalidate();
                e.Handled = true;
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                if (_dragging)
                {
                    var moved = Math.Abs(_tiltX) > 2 || Math.Abs(_tiltY) > 2;
                    _dragging = false;
                    if (!moved)
                        Tapped?.Invoke(this, EventArgs.Empty);
                    _ = SpringBackAsync();
                }
                e.Handled = true;
                break;

            default:
                e.Handled = true;
                break;
        }
    }

    private void UpdateLight(float nx, float ny)
    {
        _lightX = Math.Clamp(nx, 0f, 1f);
        _lightY = Math.Clamp(ny, 0f, 1f);
    }

    private void ApplyTiltFromLight()
    {
        // Map light position to subtle card tilt (degrees).
        _tiltY = (_lightX - 0.5f) * 18f;
        _tiltX = (0.5f - _lightY) * 14f;
        RotationY = _tiltY;
        RotationX = _tiltX;
        // Soft shadow offset / parallax nudge.
        TranslationX = _tiltY * 0.35f;
        TranslationY = -_tiltX * 0.35f;
    }

    private void ThrottledInvalidate()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastInvalidateUtc).TotalMilliseconds < 16)
            return;
        _lastInvalidateUtc = now;
        _canvas.InvalidateSurface();
    }

    private async Task SpringBackAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var anim = new Animation();
            var fromX = RotationX;
            var fromY = RotationY;
            var fromTx = TranslationX;
            var fromTy = TranslationY;
            var fromLx = _lightX;
            var fromLy = _lightY;

            anim.Add(0, 1, new Animation(v =>
            {
                RotationX = fromX * (1 - v);
                RotationY = fromY * (1 - v);
                TranslationX = fromTx * (1 - v);
                TranslationY = fromTy * (1 - v);
                _lightX = (float)(fromLx + (0.5 - fromLx) * v);
                _lightY = (float)(fromLy + (0.35 - fromLy) * v);
                _tiltX = (float)RotationX;
                _tiltY = (float)RotationY;
                _canvas.InvalidateSurface();
            }));

            var tcs = new TaskCompletionSource();
            anim.Commit(this, "FoilSpringBack", 16, 280, Easing.SpringOut, (_, _) => tcs.TrySetResult());
            await tcs.Task;
        });
    }

    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        if (_disposed)
            return;

        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(SKColors.Transparent);

        if (info.Width <= 0 || info.Height <= 0)
            return;

        try
        {
            float radius = CornerRadius * (info.Width / Math.Max((float)Width, 1f));
            if (float.IsNaN(radius) || radius <= 0)
                radius = info.Width * 0.04f;

            // Soft drop shadow under the card quad, offset opposite the light.
            float shadowOx = (0.5f - _lightX) * 18f;
            float shadowOy = (0.5f - _lightY) * 14f + 10f;
            var shadowRect = new SKRect(10 + shadowOx, 14 + shadowOy, info.Width - 10 + shadowOx, info.Height - 6 + shadowOy);
            canvas.DrawRoundRect(shadowRect, radius, radius, _shadowPaint);

            var cardRect = new SKRect(8, 8, info.Width - 8, info.Height - 8);
            canvas.Save();
            canvas.ClipRoundRect(new SKRoundRect(cardRect, radius, radius), antialias: true);

            // Slight art parallax: shift image opposite light.
            float parallaxX = (_lightX - 0.5f) * -10f;
            float parallaxY = (_lightY - 0.5f) * -8f;
            var image = CardImage;
            if (image != null)
            {
                float scale = Math.Min(cardRect.Width / image.Width, cardRect.Height / image.Height) * 1.04f;
                float iw = image.Width * scale;
                float ih = image.Height * scale;
                float x = cardRect.MidX - iw / 2f + parallaxX;
                float y = cardRect.MidY - ih / 2f + parallaxY;
                var dest = new SKRect(x, y, x + iw, y + ih);
                var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
                canvas.DrawImage(image, dest, sampling, _imagePaint);
            }
            else
            {
                using var fill = new SKPaint { Color = Palette.Surface2.ToSKColor(), IsAntialias = true };
                canvas.DrawRoundRect(cardRect, radius, radius, fill);
            }

            if (IsFoilEnabled)
                DrawFoilOverlay(canvas, cardRect);

            canvas.Restore();

            // Thin foil rim.
            using var rim = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(1.5f, info.Width * 0.004f),
                Color = new SKColor(212, 175, 106, 160)
            };
            canvas.DrawRoundRect(cardRect, radius, radius, rim);
        }
        catch
        {
            // Avoid black screens on rare Skia failures.
        }
    }

    private void DrawFoilOverlay(SKCanvas canvas, SKRect cardRect)
    {
        // Iridescent sweep following finger.
        float cx = cardRect.Left + cardRect.Width * _lightX;
        float cy = cardRect.Top + cardRect.Height * _lightY;

        using (var shader = SKShader.CreateLinearGradient(
                   new SKPoint(cardRect.Left, cardRect.Top),
                   new SKPoint(cardRect.Right, cardRect.Bottom),
                   [
                       new SKColor(168, 135, 63, 0),
                       new SKColor(240, 224, 184, 70),
                       new SKColor(107, 158, 191, 50),
                       new SKColor(196, 120, 90, 40),
                       new SKColor(212, 175, 106, 0)
                   ],
                   [0f, 0.35f, 0.5f, 0.65f, 1f],
                   SKShaderTileMode.Clamp))
        {
            _foilPaint.Shader = shader;
            canvas.DrawRect(cardRect, _foilPaint);
            _foilPaint.Shader = null;
        }

        using var specular = SKShader.CreateRadialGradient(
            new SKPoint(cx, cy),
            cardRect.Width * 0.45f,
            [
                new SKColor(255, 250, 235, 90),
                new SKColor(212, 175, 106, 35),
                new SKColor(255, 255, 255, 0)
            ],
            [0f, 0.35f, 1f],
            SKShaderTileMode.Clamp);
        _specularPaint.Shader = specular;
        canvas.DrawRect(cardRect, _specularPaint);
        _specularPaint.Shader = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _canvas.PaintSurface -= OnPaintSurface;
        _canvas.Touch -= OnTouch;
        _imagePaint.Dispose();
        _shadowPaint.Dispose();
        _foilPaint.Dispose();
        _specularPaint.Dispose();
    }
}
