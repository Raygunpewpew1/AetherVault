using AetherVault.Core;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace AetherVault.Controls;

/// <summary>
/// Horizontal strip of mana symbol density (W U B R G + other) from deck spell mana costs.
/// </summary>
public class DeckManaPipStripView : SKCanvasView
{
    public static readonly BindableProperty PipCountsProperty = BindableProperty.Create(
        nameof(PipCounts),
        typeof(int[]),
        typeof(DeckManaPipStripView),
        null,
        propertyChanged: (b, _, _) => ((DeckManaPipStripView)b).InvalidateSurface());

    public int[]? PipCounts
    {
        get => (int[]?)GetValue(PipCountsProperty);
        set => SetValue(PipCountsProperty, value);
    }

    private static readonly SKColor[] PipColors =
    [
        new SKColor(0xF5, 0xE6, 0xC8), // W
        new SKColor(0x0E, 0x68, 0xC0), // U
        new SKColor(0x4A, 0x37, 0x5F), // B
        new SKColor(0xD8, 0x62, 0x18), // R
        new SKColor(0x00, 0x7B, 0x34), // G
        new SKColor(0x6E, 0x6E, 0x6E), // other / colorless mix
    ];

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var counts = PipCounts;
        if (counts == null || counts.Length < ManaCostPipAnalyzer.SlotCount)
            return;

        int total = 0;
        int maxCount = 1;
        for (int i = 0; i < ManaCostPipAnalyzer.SlotCount; i++)
        {
            total += counts[i];
            if (counts[i] > maxCount)
                maxCount = counts[i];
        }

        if (total <= 0)
            return;

        float w = e.Info.Width;
        float h = e.Info.Height;
        float gap = Math.Max(2f, w * 0.01f);
        float labelBand = h * 0.22f;
        float chartH = h - labelBand;
        float colW = (w - gap * (ManaCostPipAnalyzer.SlotCount - 1)) / ManaCostPipAnalyzer.SlotCount;

        using var paint = new SKPaint { IsAntialias = true };
        using var font = new SKFont { Size = Math.Max(8f, h * 0.2f) };
        using var letterPaint = new SKPaint { IsAntialias = true, Color = new SKColor(0xBB, 0xBB, 0xBB) };
        using var countPaint = new SKPaint { IsAntialias = true, Color = SKColors.White };

        ReadOnlySpan<char> wubrg = "WUBRG";
        for (int i = 0; i < ManaCostPipAnalyzer.SlotCount; i++)
        {
            float x = i * (colW + gap);
            float barH = chartH * (counts[i] / (float)maxCount);
            float y = chartH - barH;

            paint.Color = PipColors[i];
            if (barH > 0)
                canvas.DrawRoundRect(x, y, colW, barH, 3f, 3f, paint);

            if (counts[i] > 0 && barH > font.Size * 1.2f)
                canvas.DrawText(counts[i].ToString(), x + colW / 2f, y + barH * 0.55f, SKTextAlign.Center, font, countPaint);

            char letter = i < wubrg.Length ? wubrg[i] : '+';
            canvas.DrawText(letter.ToString(), x + colW / 2f, h - 3f, SKTextAlign.Center, font, letterPaint);
        }
    }
}
