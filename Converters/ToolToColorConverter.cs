using System.Globalization;
using LunaDraw.Logic.Tools;

namespace LunaDraw.Converters;

public class ToolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = false;

        if (value is IDrawingTool activeTool && parameter is string targetToolName)
        {
            if (targetToolName == "Shapes")
            {
                isActive = activeTool is RectangleTool or EllipseTool or LineTool;
            }
            else
            {
                isActive = string.Equals(activeTool.GetType().Name, targetToolName, StringComparison.Ordinal);
            }
        }

        if (isActive)
        {
            if (Application.Current?.Resources.TryGetValue("Secondary", out var color) == true)
                return color;
            return Colors.Orange; // Fallback
        }

        // Inactive Color
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var key = isDark ? "Gray700" : "Gray200";
        
        if (Application.Current?.Resources.TryGetValue(key, out var inactiveColor) == true)
            return inactiveColor;

        return isDark ? Colors.DarkGray : Colors.LightGray; // Fallback
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
