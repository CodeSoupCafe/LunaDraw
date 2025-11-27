using System.Globalization;
using LunaDraw.Logic.Tools;

namespace LunaDraw.Converters;

public class ActiveShapeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IDrawingTool tool)
        {
            if (tool is RectangleTool) return "▭";
            if (tool is EllipseTool) return "◯";
            if (tool is LineTool) return "／";
        }

        // Default icon for the shapes button if no specific shape tool is active,
        // or if the active tool is not a shape (though the text binding usually only matters when it IS a shape, 
        // or if we want to revert to the default group icon)
        return "🔷";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
