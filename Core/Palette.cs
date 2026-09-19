namespace AetherVault.Core;

/// <summary>
/// Code-side mirror of <c>Resources/Styles/AppColors.xaml</c>.
/// Keep hex values in sync with that file; do not scatter new literals elsewhere.
/// </summary>
public static class Palette
{
    public static readonly Color Primary = Color.FromArgb("#03DAC5");
    /// <summary>Darker teal for pressed/hover — not Uranium "Primary in dark theme".</summary>
    public static readonly Color PrimaryDark = Color.FromArgb("#018786");
    /// <summary>Alias of Primary until XAML call sites migrate.</summary>
    public static readonly Color Accent = Primary;
    public static readonly Color OnPrimary = Color.FromArgb("#121212");
    public static readonly Color Background = Color.FromArgb("#121212");
    public static readonly Color Surface = Color.FromArgb("#1E1E1E");
    public static readonly Color CardBackground = Color.FromArgb("#252525");
    public static readonly Color TextPrimary = Color.FromArgb("#F0F0F0");
    public static readonly Color TextSecondary = Color.FromArgb("#A0A0A0");
    public static readonly Color ErrorColor = Color.FromArgb("#CF6679");
    public static readonly Color LegalGreen = Color.FromArgb("#4CAF50");
    public static readonly Color BannedRed = Color.FromArgb("#F44336");
    public static readonly Color RestrictedYellow = Color.FromArgb("#FFC107");
    public static readonly Color RarityCommon = Color.FromArgb("#C0C0C0");
    public static readonly Color RarityUncommon = Color.FromArgb("#B0C4DE");
    public static readonly Color RarityRare = Color.FromArgb("#FFD700");
    public static readonly Color RarityMythic = Color.FromArgb("#FF8C00");

    /// <summary>Chart series (same order as ChartPalette in AppColors.xaml).</summary>
    public static readonly Color[] ChartPalette =
    [
        Color.FromArgb("#7C4DFF"),
        Color.FromArgb("#FF8A65"),
        Color.FromArgb("#42A5F5"),
        Color.FromArgb("#FFD54F"),
        Color.FromArgb("#EC407A"),
        Color.FromArgb("#AB47BC"),
        Color.FromArgb("#8D6E63"),
        Color.FromArgb("#26C6DA"),
    ];
}
