using System.Windows.Input;

namespace AetherVault.Controls;

/// <summary>
/// 48×48 toolbar icon (24px SVG via <see cref="Core.Icons"/>).
/// Tints TextSecondary by default, Primary when <see cref="IsActive"/>.
/// </summary>
public partial class IconButton : ContentView
{
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph), typeof(string), typeof(IconButton), string.Empty);

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(IconButton));

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(IconButton));

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(IconButton), false,
        propertyChanged: (b, _, _) => ((IconButton)b).UpdateTint());

    public static readonly BindableProperty BadgeCountProperty = BindableProperty.Create(
        nameof(BadgeCount), typeof(int), typeof(IconButton), 0,
        propertyChanged: (b, _, _) => ((IconButton)b).UpdateShowBadge());

    public static readonly BindableProperty ShowBadgeProperty = BindableProperty.Create(
        nameof(ShowBadge), typeof(bool), typeof(IconButton), false);

    public static readonly BindableProperty ToolTipProperty = BindableProperty.Create(
        nameof(ToolTip), typeof(string), typeof(IconButton), string.Empty);

    public static readonly BindableProperty TintColorProperty = BindableProperty.Create(
        nameof(TintColor), typeof(Color), typeof(IconButton), Colors.Gray);

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public int BadgeCount
    {
        get => (int)GetValue(BadgeCountProperty);
        set => SetValue(BadgeCountProperty, value);
    }

    public bool ShowBadge
    {
        get => (bool)GetValue(ShowBadgeProperty);
        private set => SetValue(ShowBadgeProperty, value);
    }

    public string ToolTip
    {
        get => (string)GetValue(ToolTipProperty);
        set => SetValue(ToolTipProperty, value);
    }

    public Color TintColor
    {
        get => (Color)GetValue(TintColorProperty);
        private set => SetValue(TintColorProperty, value);
    }

    public IconButton()
    {
        InitializeComponent();
        UpdateShowBadge();
        Loaded += (_, _) => UpdateTint();
    }

    private void UpdateShowBadge() => ShowBadge = BadgeCount > 0;

    private void UpdateTint()
    {
        Color secondary = Colors.Gray;
        Color primary = Colors.Teal;
        if (Application.Current?.Resources.TryGetValue("TextSecondary", out var s) == true && s is Color sc)
            secondary = sc;
        if (Application.Current?.Resources.TryGetValue("Primary", out var p) == true && p is Color pc)
            primary = pc;
        TintColor = IsActive ? primary : secondary;
    }
}
