using System.Collections.Concurrent;
using AetherVault.Models;

namespace AetherVault.ViewModels;

/// <summary>Color identity → deck list row background brushes (main/sideboard rows). Solids + static cache; no gradient fills.</summary>
public static class DeckRowStripBrushes
{
    private static ReadOnlySpan<char> WubrgOrder => "WUBRG";

    private static readonly ConcurrentDictionary<string, SolidColorBrush> BrushCache = new(StringComparer.Ordinal);

    private static string GetDeckRowColorIdentityString(Card? card)
    {
        if (card == null)
            return "";

        if (!string.IsNullOrWhiteSpace(card.ColorIdentity))
            return card.ColorIdentity;

        string type = card.CardType ?? "";
        if (type.Contains("Land", StringComparison.OrdinalIgnoreCase))
        {
            string fromProduced = NormalizeProducedManaLetters(card.ProducedMana);
            if (!string.IsNullOrEmpty(fromProduced))
                return fromProduced;

            string? basic = BasicLandColorIdentityFromName(card.Name);
            if (basic != null)
                return basic;
        }

        return card.Colors ?? "";
    }

    /// <summary>Deck list row: neutral, single pip, dual-color blend, or gold for 3+.</summary>
    public static Brush GetDeckRowStripBackgroundBrush(Card? card) =>
        GetStripBackgroundBrushFromIdentity(GetDeckRowColorIdentityString(card));

    private static string NormalizeProducedManaLetters(string? producedMana)
    {
        if (string.IsNullOrWhiteSpace(producedMana))
            return "";

        Span<char> buf = stackalloc char[5];
        int n = 0;
        foreach (char raw in producedMana)
        {
            char c = char.ToUpperInvariant(raw);
            if (c is not ('W' or 'U' or 'B' or 'R' or 'G'))
                continue;
            bool dup = false;
            for (int i = 0; i < n; i++)
            {
                if (buf[i] == c) { dup = true; break; }
            }

            if (!dup && n < buf.Length)
                buf[n++] = c;
        }

        return n == 0 ? "" : new string(buf[..n]);
    }

    private static string? BasicLandColorIdentityFromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        ReadOnlySpan<char> n = name.AsSpan().Trim();
        if (n.StartsWith("Snow-Covered ", StringComparison.OrdinalIgnoreCase))
            n = n["Snow-Covered ".Length..];

        return n.ToString() switch
        {
            "Plains" => "W",
            "Island" => "U",
            "Swamp" => "B",
            "Mountain" => "R",
            "Forest" => "G",
            "Wastes" => "",
            _ => null
        };
    }

    private static Color StripColorForWubrgLetter(char letter) =>
        char.ToUpperInvariant(letter) switch
        {
            'W' => Color.FromArgb("#3D3528"),
            'U' => Color.FromArgb("#0D2E4F"),
            'B' => Color.FromArgb("#1D1528"),
            'R' => Color.FromArgb("#4F0D0D"),
            'G' => Color.FromArgb("#0D351D"),
            _ => Color.FromArgb("#2A2A33")
        };

    private static Color AverageColors(Color a, Color b) =>
        new(
            (a.Red + b.Red) * 0.5f,
            (a.Green + b.Green) * 0.5f,
            (a.Blue + b.Blue) * 0.5f,
            (a.Alpha + b.Alpha) * 0.5f);

    public static Color GetStripBackgroundColorFromIdentity(string colorIdentity)
    {
        if (GetStripBackgroundBrushFromIdentity(colorIdentity) is SolidColorBrush scb)
            return scb.Color;
        return Color.FromArgb("#2A2A33");
    }

    public static Brush GetStripBackgroundBrushFromIdentity(string colorIdentity)
    {
        colorIdentity = (colorIdentity ?? "").ToUpperInvariant();
        bool w = colorIdentity.Contains('W');
        bool u = colorIdentity.Contains('U');
        bool b = colorIdentity.Contains('B');
        bool r = colorIdentity.Contains('R');
        bool g = colorIdentity.Contains('G');
        int count = (w ? 1 : 0) + (u ? 1 : 0) + (b ? 1 : 0) + (r ? 1 : 0) + (g ? 1 : 0);

        string key = count switch
        {
            0 => "0",
            >= 3 => "M",
            2 => BuildTwoColorKey(colorIdentity),
            _ => BuildSingleKey(w, u, b, r, g)
        };

        return BrushCache.GetOrAdd(key, static k => new SolidColorBrush(ResolveColorForKey(k)));
    }

    private static string BuildSingleKey(bool w, bool u, bool b, bool r, bool g)
    {
        if (w && !u && !b && !r && !g) return "W";
        if (u && !w && !b && !r && !g) return "U";
        if (b && !w && !u && !r && !g) return "B";
        if (r && !w && !u && !b && !g) return "R";
        if (g && !w && !u && !b && !r) return "G";
        return "0";
    }

    private static string BuildTwoColorKey(string colorIdentity)
    {
        char first = ' ';
        char second = ' ';
        int n = 0;
        foreach (char ch in WubrgOrder)
        {
            if (!colorIdentity.Contains(ch)) continue;
            if (n == 0) first = ch;
            else second = ch;
            n++;
            if (n == 2) break;
        }

        return $"{first}{second}";
    }

    private static Color ResolveColorForKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key is "0" or "  ")
            return Color.FromArgb("#2A2A33");
        if (key == "M")
            return Color.FromArgb("#3B2C0A");
        if (key.Length == 1 && key[0] is 'W' or 'U' or 'B' or 'R' or 'G')
            return StripColorForWubrgLetter(key[0]);
        if (key.Length == 2
            && key[0] is 'W' or 'U' or 'B' or 'R' or 'G'
            && key[1] is 'W' or 'U' or 'B' or 'R' or 'G')
        {
            return AverageColors(StripColorForWubrgLetter(key[0]), StripColorForWubrgLetter(key[1]));
        }

        return Color.FromArgb("#2A2A33");
    }
}
