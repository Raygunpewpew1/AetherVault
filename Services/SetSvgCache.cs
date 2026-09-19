using SkiaSharp;
using Svg.Skia;
using System.Collections.Concurrent;

namespace AetherVault.Services;

/// <summary>
/// Set symbols: memory → disk → embedded, then Scryfall in the background.
/// </summary>
public static class SetSvgCache
{
    private const string FallbackKey = "fallback";
    private const int MinRequestIntervalMs = 120;

    private static readonly object Gate = new();
    private static readonly Dictionary<string, SKSvg> Cache = new();
    private static readonly HashSet<string> Failed = [];
    private static readonly ConcurrentDictionary<string, byte> InFlight = new();

    private static readonly HttpClient Http = NetworkHelper.CreateHttpClient(TimeSpan.FromSeconds(20));
    private static readonly SemaphoreSlim DownloadLock = new(1, 1);
    private static long _lastDownloadMs;

    private static string? _cacheDir;
    private static string? _resourcePrefix;

    /// <summary>Fired on main thread after a set SVG is saved from Scryfall.</summary>
    public static event Action? SymbolsUpdated;

    public static string NormalizeSetCode(string setCode) => setCode.ToLowerInvariant();

    public static SKPicture? GetSymbol(string setCode)
    {
        if (string.IsNullOrWhiteSpace(setCode))
            return null;

        var key = NormalizeSetCode(setCode);
        var picture = TryGet(key);
        if (picture != null)
            return picture;

        QueueDownload(key);
        return TryGet(FallbackKey);
    }

    public static void DrawSymbol(SKCanvas canvas, string setCode, float x, float y, float size, SKColor? tint = null)
    {
        var picture = GetSymbol(setCode);
        if (picture == null) return;
        SvgCacheEngine.DrawPictureInRect(canvas, picture, x, y, size, tint, centerInRect: true);
    }

    public static void DrawSymbol(SKCanvas canvas, string setCode, SKRect destRect, SKColor? tint = null)
    {
        var picture = GetSymbol(setCode);
        if (picture == null) return;
        SvgCacheEngine.DrawPictureInRect(canvas, picture, destRect, tint, centerInRect: true);
    }

    public static void ClearCache()
    {
        lock (Gate)
        {
            foreach (var svg in Cache.Values)
                svg.Dispose();
            Cache.Clear();
            Failed.Clear();
        }
    }

    private static SKPicture? TryGet(string key)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(key, out var cached))
                return cached.Picture;
            if (Failed.Contains(key))
                return null;
        }

        var svg = LoadLocal(key);
        lock (Gate)
        {
            if (Cache.TryGetValue(key, out var cached))
            {
                svg?.Dispose();
                return cached.Picture;
            }

            if (svg?.Picture != null)
            {
                Cache[key] = svg;
                return svg.Picture;
            }

            svg?.Dispose();
            if (key != FallbackKey)
                Failed.Add(key);
            return null;
        }
    }

    private static SKSvg? LoadLocal(string key)
    {
        try
        {
            var path = Path.Combine(GetCacheDir(), $"{key}.svg");
            if (File.Exists(path))
            {
                var fromDisk = new SKSvg();
                fromDisk.FromSvg(File.ReadAllText(path));
                if (fromDisk.Picture != null)
                    return fromDisk;
                fromDisk.Dispose();
            }
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"SetSymbol disk load '{key}': {ex.Message}", LogLevel.Warning);
        }

        return LoadEmbedded(key);
    }

    private static SKSvg? LoadEmbedded(string key)
    {
        try
        {
            _resourcePrefix ??= FindResourcePrefix();
            if (_resourcePrefix == null)
                return null;

            var name = $"{_resourcePrefix}.{key}.svg";
            using var stream = typeof(SetSvgCache).Assembly.GetManifestResourceStream(name);
            if (stream == null)
                return null;

            using var reader = new StreamReader(stream);
            var svg = new SKSvg();
            svg.FromSvg(reader.ReadToEnd());
            return svg;
        }
        catch (Exception ex)
        {
            Logger.LogStuff($"SetSymbol embed load '{key}': {ex.Message}", LogLevel.Warning);
            return null;
        }
    }

    private static string? FindResourcePrefix()
    {
        foreach (var name in typeof(SetSvgCache).Assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!name.Contains("Assets.SVGSets", StringComparison.OrdinalIgnoreCase))
                continue;

            var withoutExt = name[..name.LastIndexOf(".svg", StringComparison.OrdinalIgnoreCase)];
            var lastDot = withoutExt.LastIndexOf('.');
            if (lastDot > 0)
                return withoutExt[..lastDot];
        }

        return null;
    }

    private static string GetCacheDir()
    {
        if (_cacheDir != null)
            return _cacheDir;

        _cacheDir = Path.Combine(FileSystem.CacheDirectory, "SetSymbols");
        Directory.CreateDirectory(_cacheDir);
        return _cacheDir;
    }

    private static void QueueDownload(string key)
    {
        if (key is FallbackKey)
            return;
        if (!InFlight.TryAdd(key, 0))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await DownloadAndStoreAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogStuff($"SetSymbol download '{key}': {ex.Message}", LogLevel.Warning);
            }
            finally
            {
                InFlight.TryRemove(key, out _);
            }
        });
    }

    private static async Task DownloadAndStoreAsync(string key)
    {
        await DownloadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var now = Environment.TickCount64;
            var wait = MinRequestIntervalMs - (now - Interlocked.Read(ref _lastDownloadMs));
            if (wait > 0)
                await Task.Delay((int)wait).ConfigureAwait(false);

            var url = $"https://svgs.scryfall.io/sets/{Uri.EscapeDataString(key)}.svg";
            var svgText = await Http.GetStringAsync(url).ConfigureAwait(false);
            Interlocked.Exchange(ref _lastDownloadMs, Environment.TickCount64);

            if (svgText.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) < 0)
                return;

            var path = Path.Combine(GetCacheDir(), $"{key}.svg");
            await File.WriteAllTextAsync(path, svgText.Trim() + "\n").ConfigureAwait(false);

            lock (Gate)
            {
                Failed.Remove(key);
                if (Cache.Remove(key, out var old))
                    old.Dispose();
            }

            MainThread.BeginInvokeOnMainThread(() => SymbolsUpdated?.Invoke());
        }
        finally
        {
            DownloadLock.Release();
        }
    }
}
