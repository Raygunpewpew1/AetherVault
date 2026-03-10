namespace AetherVault.Services;

using Plugin.Maui.Audio;

/// <summary>
/// Plays the bundled hehe.mp3 from Resources/Raw via Plugin.Maui.Audio. Fails silently if the asset is missing.
/// </summary>
public class EasterEggSoundService : IEasterEggSoundService
{
    private const string AssetName = "hehe.mp3";
    private readonly IAudioManager _audioManager;

    public EasterEggSoundService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public void Play()
    {
        _ = PlayAsync();
    }

    private async Task PlayAsync()
    {
        // Try paths that different MAUI/Android packagers might use
        string[] paths = [AssetName, "Raw/" + AssetName, "Resources/Raw/" + AssetName];
        foreach (var path in paths)
        {
            try
            {
                var stream = await FileSystem.OpenAppPackageFileAsync(path).ConfigureAwait(false);
                await PlayFromStreamAsync(stream).ConfigureAwait(false);
                return;
            }
            catch (FileNotFoundException) { }
            catch (Exception ex)
            {
                Logger.LogStuff($"[EasterEgg] OpenAppPackageFileAsync('{path}'): {ex.Message}", LogLevel.Warning);
            }
        }
        Logger.LogStuff("[EasterEgg] hehe.mp3 not found. Add Resources/Raw/hehe.mp3 and rebuild.", LogLevel.Warning);
    }

    private async Task PlayFromStreamAsync(Stream fileStream)
    {
        await using var _ = fileStream;
        var mem = new MemoryStream();
        await fileStream.CopyToAsync(mem).ConfigureAwait(false);
        mem.Position = 0;
        var player = _audioManager.CreatePlayer(mem);
        player.Play();
        // Mem stream kept alive; player may read during playback.
    }
}
