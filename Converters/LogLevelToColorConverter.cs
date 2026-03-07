namespace AetherVault.Converters;

/// <summary>
/// Converts <see cref="AetherVault.Services.LogLevel"/> to a display color for the in-app log viewer.
/// </summary>
public class LogLevelToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not AetherVault.Services.LogLevel level)
            return Colors.Gray;

        return level switch
        {
            AetherVault.Services.LogLevel.Debug => Color.FromArgb("#808080"),
            AetherVault.Services.LogLevel.Info => Color.FromArgb("#E0E0E0"),
            AetherVault.Services.LogLevel.Warning => Color.FromArgb("#FFB74D"),
            AetherVault.Services.LogLevel.Error => Color.FromArgb("#F44336"),
            _ => Colors.Gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
