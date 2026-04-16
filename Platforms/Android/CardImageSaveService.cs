using AetherVault.Services;
using Android.Content;
using Android.OS;
using Android.Provider;

namespace AetherVault.Platforms.Android;

/// <summary>
/// Saves PNG files under Pictures/AetherVault using MediaStore (scoped storage).
/// </summary>
public sealed class CardImageSaveService : ICardImageSaveService
{
    public Task<(bool success, string? error)> SavePngToGalleryAsync(byte[] pngBytes, string fileName)
    {
        if (pngBytes.Length == 0)
            return Task.FromResult((false, (string?)"No image data."));

        if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            fileName += ".png";

        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var resolver = context.ContentResolver;
                if (resolver == null)
                    return (false, (string?)"Could not access storage.");

                var values = new ContentValues();
                values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
                values.Put(MediaStore.IMediaColumns.MimeType, "image/png");

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    values.Put(
                        MediaStore.IMediaColumns.RelativePath,
                        global::Android.OS.Environment.DirectoryPictures + "/AetherVault");
                }

                var collection = MediaStore.Images.Media.ExternalContentUri;
                if (collection == null)
                    return (false, (string?)"Media store is not available.");

                var uri = resolver.Insert(collection, values);
                if (uri == null)
                    return (false, (string?)"Could not create gallery file.");

                using var stream = resolver.OpenOutputStream(uri);
                if (stream == null)
                    return (false, (string?)"Could not write file.");

                stream.Write(pngBytes);
                stream.Flush();

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    values.Clear();
                    values.Put(MediaStore.IMediaColumns.IsPending, 0);
                    resolver.Update(uri, values, null, null);
                }

                return (true, (string?)null);
            }
            catch (Exception ex)
            {
                Logger.LogStuff($"SavePngToGallery failed: {ex.Message}", LogLevel.Error);
                return (false, ex.Message);
            }
        });
    }
}
