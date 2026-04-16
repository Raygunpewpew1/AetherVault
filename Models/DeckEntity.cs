using AetherVault.Core;

namespace AetherVault.Models;

/// <summary>
/// Database entity for a Deck.
/// </summary>
public class DeckEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Format { get; set; } = DeckFormat.Standard.ToDbField();
    public string Description { get; set; } = "";

    /// <summary>Optional printing UUID for the Decks-tab tile art; overrides commander when set (see <see cref="Services.DeckBuilder.DeckBuilderService.SetDeckCoverAsync"/>).</summary>
    public string CoverCardId { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }

    // Commander specific
    public string CommanderId { get; set; } = "";
    public string CommanderName { get; set; } = "";
    public string PartnerId { get; set; } = "";
    public string ColorIdentity { get; set; } = "";

    /// <summary>
    /// Parsed WUBRG set from persisted <see cref="ColorIdentity"/> string (not a separate DB column).
    /// For the full commander+partner+commander-row union, use <c>DeckColorIdentityResolver</c> in Services.DeckBuilder.
    /// </summary>
    public ColorIdentity ParsedColorIdentity => global::AetherVault.Core.ColorIdentity.FromString(ColorIdentity);

    /// <summary>Commander deck strategy for in-app suggestions; enum name in DB (e.g. <c>Midrange</c>).</summary>
    public string CommanderArchetype { get; set; } = "Unknown";

    // Not persisted — populated at runtime
    public int CardCount { get; set; }

    public bool HasCommander => !string.IsNullOrEmpty(CommanderName);
    public string CommanderDisplay => HasCommander ? $"☆ {CommanderName}" : "";
    public string FormatDisplay => EnumExtensions.ParseDeckFormat(Format).ToDisplayName();

    /// <summary>
    /// Scryfall CDN key for the deck hub tile (same convention as <see cref="Card.ImageId"/>).
    /// Not stored in SQLite — set by <see cref="Services.DeckBuilder.DeckBuilderService.GetDecksAsync"/>.
    /// </summary>
    public string PreviewImageId { get; set; } = "";

    public bool HasPreviewImage =>
        !string.IsNullOrEmpty(CommanderId) || !string.IsNullOrEmpty(CoverCardId);

    /// <summary>Braced mana symbols for <c>ManaCostView</c> (e.g. <c>{W}{U}</c>).</summary>
    public string ColorIdentityManaText
    {
        get
        {
            var id = ParsedColorIdentity;
            var s = id.AsString();
            if (string.IsNullOrEmpty(s)) return "";
            return string.Concat(s.Select(c => $"{{{char.ToUpperInvariant(c)}}}"));
        }
    }

    public bool HasColorIdentity => ParsedColorIdentity.Count > 0;
}
