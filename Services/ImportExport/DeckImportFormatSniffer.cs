using System.Text;

namespace AetherVault.Services.ImportExport;

/// <summary>
/// Detects AetherVault deck CSV vs plain-text deck lists when the file picker
/// does not provide a reliable extension (common on Android content URIs).
/// </summary>
public static class DeckImportFormatSniffer
{
    /// <summary>Reject absurdly large files to avoid OOM on mobile.</summary>
    public const long MaxDeckImportBytes = 15 * 1024 * 1024;

    public enum DeckImportKind
    {
        Csv,
        Txt,
    }

    /// <summary>
    /// Copies <paramref name="source"/> into a new memory stream (bounded by <see cref="MaxDeckImportBytes"/>).
    /// </summary>
    public static async Task<MemoryStream> BufferEntireStreamAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > MaxDeckImportBytes)
                throw new InvalidOperationException($"Deck file is larger than {MaxDeckImportBytes / (1024 * 1024)} MB.");
            await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Chooses import path from extension, or from first non-empty content line.
    /// </summary>
    public static DeckImportKind DetectFormat(string? fileName, MemoryStream bufferedUtf8Content)
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (ext == ".csv")
            return DeckImportKind.Csv;
        if (ext == ".txt" || ext == ".dec")
            return DeckImportKind.Txt;

        bufferedUtf8Content.Position = 0;
        if (FirstNonEmptyLineLooksLikeDeckCsvHeader(bufferedUtf8Content))
            return DeckImportKind.Csv;

        return DeckImportKind.Txt;
    }

    /// <summary>
    /// Reads the stream from current position; restores position to 0.
    /// </summary>
    internal static bool FirstNonEmptyLineLooksLikeDeckCsvHeader(Stream utf8Stream)
    {
        long start = utf8Stream.Position;
        try
        {
            using var reader = new StreamReader(utf8Stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            while (reader.ReadLine() is { } line)
            {
                var t = line.Trim();
                if (t.Length == 0)
                    continue;
                if (t.StartsWith('#') || t.StartsWith("//", StringComparison.Ordinal))
                    continue;

                return LineLooksLikeAetherVaultDeckCsvHeader(t);
            }

            return false;
        }
        finally
        {
            utf8Stream.Position = 0;
        }
    }

    private static bool LineLooksLikeAetherVaultDeckCsvHeader(string line)
    {
        // Exported CSV: Source,Deck Name,Format,... — require the canonical column title.
        foreach (var raw in SplitCsvHeaderCells(line))
        {
            var cell = NormalizeHeaderCell(raw);
            if (cell.Equals(DeckCsvV1.DeckName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> SplitCsvHeaderCells(string line)
    {
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                    inQuotes = true;
                else if (c == ',')
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        yield return sb.ToString();
    }

    private static string NormalizeHeaderCell(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            s = s[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
        return s.Trim();
    }
}
