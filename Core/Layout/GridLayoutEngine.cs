using SkiaSharp;
using System.Collections.Immutable;

namespace AetherVault.Core.Layout;

public abstract record RenderCommand;
public record DrawCardCommand(CardState Card, SKRect Rect, int Index) : RenderCommand;

public record RenderList(
    ImmutableArray<RenderCommand> Commands,
    float TotalHeight,
    int VisibleStart,
    int VisibleEnd,
    float CardWidth,
    float CardHeight,
    ViewMode ViewMode = ViewMode.Grid
)
{
    public static RenderList Empty => new(
        ImmutableArray<RenderCommand>.Empty,
        0, 0, -1, 0, 0
    );
}

public static class GridLayoutEngine
{
    // Constants for list view row sizing
    private const float ListRowHeight = 96f;
    private const float ListImgWidth = 55f;
    private const float TextRowHeight = 50f;

    public static RenderList Calculate(GridState state)
    {
        if (state.Config.ViewMode == ViewMode.List)
            return CalculateList(state);
        if (state.Config.ViewMode == ViewMode.TextOnly)
            return CalculateTextOnly(state);

        return CalculateGrid(state);
    }

    /// <summary>
    /// Layout rectangle in scroll/world space for any card index (not limited to the visible range).
    /// Used for effects that must anchor to a card that may be off-screen in <see cref="RenderList.Commands"/>.
    /// </summary>
    public static bool TryGetWorldRectForCardIndex(GridState state, int index, out SKRect rect)
    {
        rect = default;
        var cards = state.Cards;
        if (index < 0 || index >= cards.Length) return false;

        var viewport = state.Viewport;
        var config = state.Config;
        float width = viewport.Width > 0 ? viewport.Width : 360f;

        switch (config.ViewMode)
        {
            case ViewMode.List:
                {
                    float y = index * ListRowHeight;
                    rect = new SKRect(0f, y, width, y + ListRowHeight);
                    return true;
                }
            case ViewMode.TextOnly:
                {
                    float y = index * TextRowHeight;
                    rect = new SKRect(0f, y, width, y + TextRowHeight);
                    return true;
                }
            default:
                {
                    float availWidth = width - 20f;
                    int columns = Math.Max(1, (int)((availWidth - config.CardSpacing) / (config.MinCardWidth + config.CardSpacing)));
                    float cardWidth = (availWidth - config.CardSpacing * (columns + 1)) / columns;
                    float cardHeight = cardWidth * config.CardImageRatio + config.LabelHeight;
                    float rowHeight = cardHeight + config.CardSpacing;
                    int row = index / columns;
                    int col = index % columns;
                    float x = 10f + config.CardSpacing + col * (cardWidth + config.CardSpacing);
                    float y = config.CardSpacing + row * rowHeight;
                    rect = new SKRect(x, y, x + cardWidth, y + cardHeight);
                    return true;
                }
        }
    }

    private static RenderList CalculateGrid(GridState state)
    {
        var config = state.Config;
        var viewport = state.Viewport;
        var cards = state.Cards;

        float width = viewport.Width;
        if (width <= 0) width = 360f;

        // 1. Calculate Columns
        float availWidth = width - 20f; // 10px padding each side
        int columns = Math.Max(1, (int)((availWidth - config.CardSpacing) / (config.MinCardWidth + config.CardSpacing)));

        // 2. Calculate Card Dimensions
        float cardWidth = (availWidth - config.CardSpacing * (columns + 1)) / columns;
        float cardHeight = cardWidth * config.CardImageRatio + config.LabelHeight;
        float rowHeight = cardHeight + config.CardSpacing;

        int count = cards.Length;

        // FIX #6: Return Empty with zero height so spacer resets on ClearCards
        if (count == 0)
            return RenderList.Empty;

        // 3. Calculate Total Height
        int rowCount = (int)Math.Ceiling((double)count / columns);
        float totalHeight = rowCount * rowHeight + config.CardSpacing + 50f;

        // 4. Calculate Visible Range
        float effectiveOffset = Math.Max(0, viewport.ScrollY);
        float viewportHeight = viewport.Height > 0 ? viewport.Height : 1000f;

        int firstRow = Math.Max(0, (int)((effectiveOffset - config.CardSpacing) / rowHeight));

        // FIX #8: removed the extra "+ 1" on lastRow — one extra row of buffer is enough
        int lastRow = (int)((effectiveOffset + viewportHeight + config.CardSpacing) / rowHeight);

        int visibleStart = Math.Max(0, Math.Min(count - 1, firstRow * columns));
        int visibleEnd = Math.Max(0, Math.Min(count - 1, (lastRow + 1) * columns - 1));

        if (visibleStart >= count)
            return new RenderList(ImmutableArray<RenderCommand>.Empty, totalHeight, 0, -1, cardWidth, cardHeight);

        // 5. Generate Commands
        var commands = ImmutableArray.CreateBuilder<RenderCommand>(visibleEnd - visibleStart + 1);

        for (int i = visibleStart; i <= visibleEnd; i++)
        {
            var card = cards[i];
            int row = i / columns;
            int col = i % columns;

            // FIX #6 (layout): x uses only CardSpacing as the left margin — the 10px outer
            // padding is already baked into availWidth. Adding both caused double-padding.
            float x = 10f + config.CardSpacing + col * (cardWidth + config.CardSpacing);
            float y = config.CardSpacing + row * rowHeight;

            var rect = new SKRect(x, y, x + cardWidth, y + cardHeight);
            commands.Add(new DrawCardCommand(card, rect, i));
        }

        return new RenderList(
            commands.ToImmutable(),
            totalHeight,
            visibleStart,
            visibleEnd,
            cardWidth,
            cardHeight
        );
    }

    private static RenderList CalculateList(GridState state)
    {
        var viewport = state.Viewport;
        var cards = state.Cards;
        int count = cards.Length;

        if (count == 0)
            return RenderList.Empty;

        float width = viewport.Width > 0 ? viewport.Width : 360f;
        float totalHeight = count * ListRowHeight + 50f;

        float effectiveOffset = Math.Max(0, viewport.ScrollY);
        float viewportHeight = viewport.Height > 0 ? viewport.Height : 1000f;

        int visibleStart = Math.Max(0, (int)(effectiveOffset / ListRowHeight));
        int visibleEnd = Math.Min(count - 1, (int)((effectiveOffset + viewportHeight) / ListRowHeight) + 1);

        if (visibleStart >= count)
            return new RenderList(ImmutableArray<RenderCommand>.Empty, totalHeight, 0, -1, width, ListRowHeight, ViewMode.List);

        var commands = ImmutableArray.CreateBuilder<RenderCommand>(visibleEnd - visibleStart + 1);

        for (int i = visibleStart; i <= visibleEnd; i++)
        {
            float y = i * ListRowHeight;
            var rect = new SKRect(0f, y, width, y + ListRowHeight);
            commands.Add(new DrawCardCommand(cards[i], rect, i));
        }

        return new RenderList(
            commands.ToImmutable(),
            totalHeight,
            visibleStart,
            visibleEnd,
            width,
            ListRowHeight,
            ViewMode.List
        );
    }

    private static RenderList CalculateTextOnly(GridState state)
    {
        var viewport = state.Viewport;
        var cards = state.Cards;
        int count = cards.Length;

        if (count == 0)
            return RenderList.Empty;

        float width = viewport.Width > 0 ? viewport.Width : 360f;
        float totalHeight = count * TextRowHeight + 50f;

        float effectiveOffset = Math.Max(0, viewport.ScrollY);
        float viewportHeight = viewport.Height > 0 ? viewport.Height : 1000f;

        int visibleStart = Math.Max(0, (int)(effectiveOffset / TextRowHeight));
        int visibleEnd = Math.Min(count - 1, (int)((effectiveOffset + viewportHeight) / TextRowHeight) + 1);

        if (visibleStart >= count)
            return new RenderList(ImmutableArray<RenderCommand>.Empty, totalHeight, 0, -1, width, TextRowHeight, ViewMode.TextOnly);

        var commands = ImmutableArray.CreateBuilder<RenderCommand>(visibleEnd - visibleStart + 1);

        for (int i = visibleStart; i <= visibleEnd; i++)
        {
            float y = i * TextRowHeight;
            var rect = new SKRect(0f, y, width, y + TextRowHeight);
            commands.Add(new DrawCardCommand(cards[i], rect, i));
        }

        return new RenderList(
            commands.ToImmutable(),
            totalHeight,
            visibleStart,
            visibleEnd,
            width,
            TextRowHeight,
            ViewMode.TextOnly
        );
    }
}