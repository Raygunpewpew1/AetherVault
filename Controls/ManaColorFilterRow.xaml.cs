using System.Collections;
using System.Windows.Input;

namespace AetherVault.Controls;

public partial class ManaColorFilterRow : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(ManaColorFilterRow));

    public static readonly BindableProperty ToggleColorCommandProperty = BindableProperty.Create(
        nameof(ToggleColorCommand),
        typeof(ICommand),
        typeof(ManaColorFilterRow));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? ToggleColorCommand
    {
        get => (ICommand?)GetValue(ToggleColorCommandProperty);
        set => SetValue(ToggleColorCommandProperty, value);
    }

    public ManaColorFilterRow()
    {
        InitializeComponent();
    }
}
