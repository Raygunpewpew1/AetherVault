namespace AetherVault.Core;

/// <summary>
/// Code-side mirror of <c>Resources/Styles/AppColors.xaml</c> (Vault &amp; Foil).
/// Keep hex values in sync with that file; do not scatter new literals elsewhere.
/// </summary>
public static class Palette
{
    public static readonly Color Primary = Color.FromArgb("#D4AF6A");
    /// <summary>Darker foil for pressed/hover.</summary>
    public static readonly Color PrimaryDark = Color.FromArgb("#A8873F");
    /// <summary>Alias of Primary.</summary>
    public static readonly Color Accent = Primary;
    public static readonly Color OnPrimary = Color.FromArgb("#0B0A0F");
    public static readonly Color Background = Color.FromArgb("#0B0A0F");
    public static readonly Color Surface0 = Color.FromArgb("#0B0A0F");
    public static readonly Color Surface1 = Color.FromArgb("#14121A");
    public static readonly Color Surface2 = Color.FromArgb("#1C1A24");
    public static readonly Color Surface = Surface1;
    public static readonly Color CardBackground = Surface2;
    public static readonly Color BorderSubtle = Color.FromArgb("#2A2734");
    public static readonly Color FoilHighlight = Color.FromArgb("#F0E0B8");
    public static readonly Color TextPrimary = Color.FromArgb("#F3EFE6");
    public static readonly Color TextSecondary = Color.FromArgb("#9A9488");
    public static readonly Color TextMuted = Color.FromArgb("#6E685E");
    public static readonly Color ErrorColor = Color.FromArgb("#E07A7A");
    public static readonly Color LegalGreen = Color.FromArgb("#6BBF7A");
    public static readonly Color BannedRed = Color.FromArgb("#E07070");
    public static readonly Color RestrictedYellow = Color.FromArgb("#E0C060");
    public static readonly Color RarityCommon = Color.FromArgb("#C0C0C0");
    public static readonly Color RarityUncommon = Color.FromArgb("#B0C4DE");
    public static readonly Color RarityRare = Color.FromArgb("#FFD700");
    public static readonly Color RarityMythic = Color.FromArgb("#FF8C00");

    /// <summary>Chart series (same order as ChartPalette in AppColors.xaml).</summary>
    public static readonly Color[] ChartPalette =
    [
        Color.FromArgb("#D4AF6A"),
        Color.FromArgb("#C4785A"),
        Color.FromArgb("#6B9EBF"),
        Color.FromArgb("#E0C060"),
        Color.FromArgb("#A87A9A"),
        Color.FromArgb("#7A9A7A"),
        Color.FromArgb("#8D6E63"),
        Color.FromArgb("#9A9488"),
    ];
}
