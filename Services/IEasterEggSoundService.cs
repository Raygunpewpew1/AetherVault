namespace AetherVault.Services;

/// <summary>
/// Plays the easter egg sound (e.g. from a hidden tap sequence). No-op if the asset is missing.
/// </summary>
public interface IEasterEggSoundService
{
    /// <summary>
    /// Plays the easter egg MP3 once. Safe to call from any thread; runs playback asynchronously.
    /// </summary>
    void Play();
}
