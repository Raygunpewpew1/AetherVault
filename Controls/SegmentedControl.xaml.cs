using System.Collections;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace AetherVault.Controls;

/// <summary>
/// Pill segmented control. Selected: Primary text on Primary @ ~15% opacity.
/// Unselected: TextSecondary. No solid teal fill.
/// </summary>
public partial class SegmentedControl : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IList), typeof(SegmentedControl), null,
        propertyChanged: (b, _, _) => ((SegmentedControl)b).Rebuild());

    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(SegmentedControl), 0,
        BindingMode.TwoWay, propertyChanged: (b, _, _) => ((SegmentedControl)b).ApplySelection());

    public static readonly BindableProperty SelectionChangedCommandProperty = BindableProperty.Create(
        nameof(SelectionChangedCommand), typeof(ICommand), typeof(SegmentedControl));

    public IList? ItemsSource
    {
        get => (IList?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    private Color _primary = Colors.Gray;
    private Color _primaryMuted = Colors.Transparent;
    private Color _textSecondary = Colors.Gray;
    private readonly List<Border> _segments = [];

    public SegmentedControl()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ResolveColors();
            Rebuild();
        };
    }

    private void ResolveColors()
    {
        if (Application.Current?.Resources.TryGetValue("Primary", out var p) == true && p is Color pc)
            _primary = pc;
        if (Application.Current?.Resources.TryGetValue("TextSecondary", out var t) == true && t is Color tc)
            _textSecondary = tc;
        _primaryMuted = _primary.WithAlpha(0.15f);
    }

    private void Rebuild()
    {
        if (SegmentsHost is null)
            return;

        SegmentsHost.Children.Clear();
        SegmentsHost.ColumnDefinitions.Clear();
        _segments.Clear();
        if (ItemsSource is null || ItemsSource.Count == 0)
            return;

        ResolveColors();
        double bodySize = GetTokenDouble("FontSizeBody", 14);
        double padH = GetTokenDouble("Spacing12", 12);

        for (var i = 0; i < ItemsSource.Count; i++)
        {
            var index = i;
            SegmentsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var label = new Label
            {
                Text = ItemsSource[i]?.ToString() ?? "",
                FontSize = bodySize,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

            var border = new Border
            {
                StrokeThickness = 0,
                Padding = new Thickness(padH, 0),
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
                Content = label,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            border.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => Select(index))
            });

            Grid.SetColumn(border, i);
            SegmentsHost.Children.Add(border);
            _segments.Add(border);
        }

        ApplySelection();
    }

    private void Select(int index)
    {
        if (index == SelectedIndex)
            return;
        SelectedIndex = index;
        if (SelectionChangedCommand?.CanExecute(index) == true)
            SelectionChangedCommand.Execute(index);
    }

    private void ApplySelection()
    {
        for (var i = 0; i < _segments.Count; i++)
        {
            var selected = i == SelectedIndex;
            _segments[i].BackgroundColor = selected ? _primaryMuted : Colors.Transparent;
            if (_segments[i].Content is Label label)
                label.TextColor = selected ? _primary : _textSecondary;
        }
    }

    private static double GetTokenDouble(string key, double fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var v) == true && v is double d)
            return d;
        return fallback;
    }
}
