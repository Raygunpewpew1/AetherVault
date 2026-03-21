namespace AetherVault.Data;

/// <summary>
/// Lightweight card metadata used for high-speed CSV import matching.
/// </summary>
public sealed class ImportLookupRow
{
    public string Uuid { get; set; } = "";
    public string Name { get; set; } = "";
    public string? FaceName { get; set; }
    public string SetCode { get; set; } = "";
    public string? SetName { get; set; }
    public string? Number { get; set; }
    public string? ScryfallId { get; set; }
}
