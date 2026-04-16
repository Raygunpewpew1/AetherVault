using AetherVault.Models;
using AetherVault.Services;
using System.Collections.Immutable;

namespace AetherVault.Core.Layout;

public enum ViewMode { Grid, List, TextOnly }

public readonly record struct CardId(string Value);

/// <summary>
/// Immutable representation of a card's data for rendering.
/// </summary>
public record CardState(
    CardId Id,
    string Name,
    string SetCode,
    string Number,
    string ScryfallId,
    int Quantity,
    bool IsOnlineOnly,
    string TypeLine = "",
    CardPriceData? PriceData = null,
    string CachedDisplayPrice = "",
    string ManaCost = "",
    CardRarity Rarity = CardRarity.Common,
    double? ReferenceUnitPriceUsd = null,
    bool CollectionRowIsFoil = false,
    bool CollectionRowIsEtched = false,
    string CollectionPriceChangeLabel = ""
)
{
    // Factory method to create state and pre-calculate display price
    public static CardState FromCard(Card card, int quantity = 0, CardPriceData? prices = null)
    {
        var state = new CardState(
            new CardId(card.Uuid),
            card.Name,
            card.SetCode,
            card.Number,
            card.ScryfallId,
            quantity,
            card.IsOnlineOnly,
            card.CardType,
            prices,
            "",
            card.ManaCost,
            card.Rarity,
            null,
            false,
            false,
            ""
        );
        return state with { CachedDisplayPrice = state.GetDisplayPrice() };
    }

    /// <summary>Grid state for an owned collection row (baseline + finish for % vs current price).</summary>
    public static CardState FromCollectionItem(CollectionItem item, CardPriceData? prices = null)
    {
        var state = FromCard(item.Card, item.Quantity, prices);
        return state with
        {
            ReferenceUnitPriceUsd = item.ReferencePriceUsd,
            CollectionRowIsFoil = item.IsFoil,
            CollectionRowIsEtched = item.IsEtched,
            CollectionPriceChangeLabel = PriceDisplayHelper.FormatCollectionPriceChangeLabel(
                item.ReferencePriceUsd,
                prices,
                item.IsFoil,
                item.IsEtched),
        };
    }

    // Helper to calculate display price if not provided (uses user's vendor priority)
    public string GetDisplayPrice()
    {
        if (!string.IsNullOrEmpty(CachedDisplayPrice)) return CachedDisplayPrice;
        return PriceDisplayHelper.GetDisplayPrice(PriceData);
    }
}

public record GridConfig(
    float MinCardWidth = 100f,
    float CardSpacing = 8f,
    float LabelHeight = 42f,
    float CardImageRatio = 1.3968f,
    int OffscreenBuffer = 40,
    ViewMode ViewMode = ViewMode.Grid
);

public record Viewport(
    float Width,
    float Height,
    float ScrollY
);

public record GridState(
    ImmutableArray<CardState> Cards,
    GridConfig Config,
    Viewport Viewport
)
{
    public static GridState Empty => new(
        ImmutableArray<CardState>.Empty,
        new GridConfig(),
        new Viewport(0, 0, 0)
    );
}

/// <summary>
/// Tracks the current drag-and-drop state for the card grid.
/// Coordinates are in world (scroll-space) units matching the canvas coordinate system.
/// </summary>
public record DragState(
    int SourceIndex,
    int TargetIndex,
    float CanvasX,
    float CanvasY,
    CardState? DraggedCard = null
);
