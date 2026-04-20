using AetherVault.Models;

namespace AetherVault.ViewModels;

/// <summary>Color identity → deck list row background brushes (main/sideboard rows).</summary>
public static class DeckRowStripBrushes
{
    /// <summary>
    /// WUBRG letters for consistent ordering on dual-color gradient strips (matches typical guild/shard display order).
    /// </summary>
    private static ReadOnlySpan<char> WubrgOrder => "WUBRG";

    /// <summary>
    /// Resolves identity text for strip coloring: <see cref="Card.ColorIdentity"/> when set; for lands, produced mana or basic name; else <see cref="Card.Colors"/>.
    /// </summary>
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

    /// <summary>Deck list row background: 0 = neutral, 1 = single pip tint, 2 = horizontal dual gradient (WUBRG order), 3+ = gold.</summary>
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

    /// <summary>Dark solid for commander header / single-color strips; dual colors use <see cref="GetStripBackgroundBrushFromIdentity"/>.</summary>
    public static Color GetStripBackgroundColorFromIdentity(string colorIdentity)
    {
        var brush = GetStripBackgroundBrushFromIdentity(colorIdentity);
        if (brush is SolidColorBrush scb)
            return scb.Color;
        if (brush is LinearGradientBrush lgb && lgb.GradientStops.Count > 0)
            return lgb.GradientStops[0].Color;
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

        if (count == 0)
            return new SolidColorBrush(Color.FromArgb("#2A2A33"));
        if (count >= 3)
            return new SolidColorBrush(Color.FromArgb("#3B2C0A"));

        if (count == 2)
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

            Color c0 = StripColorForWubrgLetter(first);
            Color c1 = StripColorForWubrgLetter(second);
            return new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
                GradientStops =
                [
                    new GradientStop(c0, 0f),
                    new GradientStop(c1, 1f)
                ]
            };
        }

        // Single color
        if (w && !u && !b && !r && !g) return new SolidColorBrush(StripColorForWubrgLetter('W'));
        if (u && !w && !b && !r && !g) return new SolidColorBrush(StripColorForWubrgLetter('U'));
        if (b && !w && !u && !r && !g) return new SolidColorBrush(StripColorForWubrgLetter('B'));
        if (r && !w && !u && !b && !g) return new SolidColorBrush(StripColorForWubrgLetter('R'));
        if (g && !w && !u && !b && !r) return new SolidColorBrush(StripColorForWubrgLetter('G'));

        return new SolidColorBrush(Color.FromArgb("#2A2A33"));
    }
}
