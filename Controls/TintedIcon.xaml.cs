namespace AetherVault.Controls;

/// <summary>24px (default) MauiImage SVG tinted with <c>IconTintColorBehavior</c>.</summary>
public partial class TintedIcon : ContentView
{
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph), typeof(string), typeof(TintedIcon), string.Empty);

    public static readonly BindableProperty TintColorProperty = BindableProperty.Create(
        nameof(TintColor), typeof(Color), typeof(TintedIcon), Colors.Gray);

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(TintedIcon), 24.0);

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public Color TintColor
    {
        get => (Color)GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public TintedIcon()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (TintColor == Colors.Gray
                && Application.Current?.Resources.TryGetValue("TextSecondary", out var s) == true
                && s is Color c)
            {
                TintColor = c;
            }
        };
    }
}
