namespace AetherVault.Services;

/// <summary>
/// Saves card artwork to the device (e.g. Android gallery).
/// </summary>
public interface ICardImageSaveService
{
    /// <summary>
    /// Writes PNG bytes to public pictures storage. <paramref name="fileName"/> should end with .png.
    /// </summary>
    Task<(bool success, string? error)> SavePngToGalleryAsync(byte[] pngBytes, string fileName);
}
