namespace AetherVault.Controls;

/// <summary>
/// Reusable empty state: tinted SVG icon (<see cref="Core.Icons"/>) + message + optional secondary.
/// </summary>
public partial class EmptyStateView : ContentView
{
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message), typeof(string), typeof(EmptyStateView), string.Empty);

    public static readonly BindableProperty SecondaryMessageProperty = BindableProperty.Create(
        nameof(SecondaryMessage), typeof(string), typeof(EmptyStateView), string.Empty);

    public static readonly BindableProperty IconGlyphProperty = BindableProperty.Create(
        nameof(IconGlyph), typeof(string), typeof(EmptyStateView), string.Empty,
        propertyChanged: (b, _, _) => ((EmptyStateView)b).UpdateIconVisibility());

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string SecondaryMessage
    {
        get => (string)GetValue(SecondaryMessageProperty);
        set => SetValue(SecondaryMessageProperty, value);
    }

    /// <summary>MauiImage SVG name from <see cref="Core.Icons"/>.</summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public EmptyStateView()
    {
        InitializeComponent();
    }

    private void UpdateIconVisibility()
    {
        if (IconImage != null)
            IconImage.IsVisible = !string.IsNullOrEmpty(IconGlyph);
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        UpdateIconVisibility();
    }
}
