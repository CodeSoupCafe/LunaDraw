using System.Globalization;
using LunaDraw.Logic.Tools;

namespace LunaDraw.Converters;

public class IsToolActiveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IDrawingTool activeTool)
            return false;

        if (parameter is string targetToolName)
        {
            // Handle special case for "Shapes" group
            if (targetToolName == "Shapes")
            {
                return activeTool is RectangleTool or EllipseTool or LineTool;
            }

            var activeTypeName = activeTool.GetType().Name;
            return string.Equals(activeTypeName, targetToolName, StringComparison.Ordinal);
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
