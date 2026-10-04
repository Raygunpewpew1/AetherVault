using System.Windows.Input;

namespace AetherVault.Controls;

/// <summary>Full-width search field with a trailing slot for icon buttons.</summary>
public partial class SearchBarRow : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(SearchBarRow), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(SearchBarRow), "Search…");

    public static readonly BindableProperty ReturnCommandProperty = BindableProperty.Create(
        nameof(ReturnCommand), typeof(ICommand), typeof(SearchBarRow));

    public static readonly BindableProperty ActionsProperty = BindableProperty.Create(
        nameof(Actions), typeof(View), typeof(SearchBarRow),
        propertyChanged: (b, _, n) => ((SearchBarRow)b).ActionsHost.Content = n as View);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public ICommand? ReturnCommand
    {
        get => (ICommand?)GetValue(ReturnCommandProperty);
        set => SetValue(ReturnCommandProperty, value);
    }

    public View? Actions
    {
        get => (View?)GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    public SearchBarRow()
    {
        InitializeComponent();
    }

    public void FocusSearch() => SearchEntry.Focus();
}
