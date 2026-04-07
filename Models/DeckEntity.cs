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
    public string CoverCardId { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }

    // Commander specific
    public string CommanderId { get; set; } = "";
    public string CommanderName { get; set; } = "";
    public string PartnerId { get; set; } = "";
    public string ColorIdentity { get; set; } = "";

    // Not persisted — populated at runtime
    public int CardCount { get; set; }

    public bool HasCommander => !string.IsNullOrEmpty(CommanderName);
    public string CommanderDisplay => HasCommander ? $"☆ {CommanderName}" : "";
    public string FormatDisplay => EnumExtensions.ParseDeckFormat(Format).ToDisplayName();

    /// <summary>UUID for hub thumbnail: commander printing, else cover card.</summary>
    public string PreviewImageUuid =>
        !string.IsNullOrEmpty(CommanderId) ? CommanderId :
        !string.IsNullOrEmpty(CoverCardId) ? CoverCardId : "";

    public bool HasPreviewImage => !string.IsNullOrEmpty(PreviewImageUuid);

    /// <summary>Braced mana symbols for <c>ManaCostView</c> (e.g. <c>{W}{U}</c>).</summary>
    public string ColorIdentityManaText
    {
        get
        {
            var id = global::AetherVault.Core.ColorIdentity.FromString(ColorIdentity);
            var s = id.AsString();
            if (string.IsNullOrEmpty(s)) return "";
            return string.Concat(s.Select(c => $"{{{char.ToUpperInvariant(c)}}}"));
        }
    }

    public bool HasColorIdentity => global::AetherVault.Core.ColorIdentity.FromString(ColorIdentity).Count > 0;
}
