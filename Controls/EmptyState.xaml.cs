namespace AetherVault.Controls;

/// <summary>Centered icon + message in TextSecondary for idle / empty lists.</summary>
public partial class EmptyState : ContentView
{
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message), typeof(string), typeof(EmptyState), string.Empty);

    public static readonly BindableProperty IconGlyphProperty = BindableProperty.Create(
        nameof(IconGlyph), typeof(string), typeof(EmptyState), string.Empty,
        propertyChanged: (b, _, _) => ((EmptyState)b).UpdateHasIcon());

    public static readonly BindableProperty HasIconProperty = BindableProperty.Create(
        nameof(HasIcon), typeof(bool), typeof(EmptyState), false);

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public bool HasIcon
    {
        get => (bool)GetValue(HasIconProperty);
        private set => SetValue(HasIconProperty, value);
    }

    public EmptyState()
    {
        InitializeComponent();
        UpdateHasIcon();
    }

    private void UpdateHasIcon() => HasIcon = !string.IsNullOrEmpty(IconGlyph);
}
