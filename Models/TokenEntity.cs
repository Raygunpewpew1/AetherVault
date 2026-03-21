namespace AetherVault.Models;

public class TokenEntity
{
    public string Artist { get; set; } = string.Empty;
    public string[] ArtistIds { get; set; } = [];
    public string AsciiName { get; set; } = string.Empty;
    public string[] AttractionLights { get; set; } = [];
    public string[] Availability { get; set; } = [];
    public string[] BoosterTypes { get; set; } = [];
    public string BorderColor { get; set; } = string.Empty;
    public string[] ColorIdentity { get; set; } = [];
    public string[] ColorIndicator { get; set; } = [];
    public string[] Colors { get; set; } = [];
    public double? EdhrecSaltiness { get; set; }
    public string FaceName { get; set; } = string.Empty;
    public string[] Finishes { get; set; } = [];
    public string FlavorName { get; set; } = string.Empty;
    public string FlavorText { get; set; } = string.Empty;
    public string[] FrameEffects { get; set; } = [];
    public string FrameVersion { get; set; } = string.Empty;
    public bool? IsFullArt { get; set; }
    public bool? IsFunny { get; set; }
    public bool? IsOversized { get; set; }
    public bool? IsPromo { get; set; }
    public bool? IsReprint { get; set; }
    public bool? IsTextless { get; set; }
    public string[] Keywords { get; set; } = [];
    public string Language { get; set; } = string.Empty;
    public string Layout { get; set; } = string.Empty;
    public string ManaCost { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Orientation { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string[] OtherFaceIds { get; set; } = [];
    public string Power { get; set; } = string.Empty;
    public string PrintedType { get; set; } = string.Empty;
    public string[] ProducedMana { get; set; } = [];
    public string[] PromoTypes { get; set; } = [];
    public string[] RelatedCards { get; set; } = [];
    public string SecurityStamp { get; set; } = string.Empty;
    public string SetCode { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string[] SourceProducts { get; set; } = [];
    public string[] Subtypes { get; set; } = [];
    public string[] Supertypes { get; set; } = [];
    public string Text { get; set; } = string.Empty;
    public string Toughness { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string[] Types { get; set; } = [];
    public string Uuid { get; set; } = string.Empty;
    public string Watermark { get; set; } = string.Empty;
}
