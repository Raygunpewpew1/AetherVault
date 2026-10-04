namespace AetherVault.Controls;

/// <summary>Page title plus optional trailing actions — shared chrome for tab/detail headers.</summary>
public partial class PageHeader : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(PageHeader), string.Empty);

    public static readonly BindableProperty TrailingContentProperty = BindableProperty.Create(
        nameof(TrailingContent), typeof(View), typeof(PageHeader),
        propertyChanged: (b, _, n) => ((PageHeader)b).TrailingHost.Content = n as View);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public View? TrailingContent
    {
        get => (View?)GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public PageHeader()
    {
        InitializeComponent();
    }
}
