using System.Globalization;

namespace LunaDraw.Converters
{
  // Simple converter that maps tool names to an icon glyph (emoji for now).
  // This keeps UI independent of a specific icon font and allows swapping to Syncfusion glyphs later.
  public class ToolNameToIconConverter : IValueConverter
  {
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Select", "🔲" },
            { "Line", "／" },
            { "Rectangle", "▭" },
            { "Ellipse", "◯" },
            { "Freehand", "✏️" },
            { "Eraser", "🧽" },
            { "Fill", "🖌️" },
            // Fallback for unknown tools
            { "Default", "🔧" }
        };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value is string name && !string.IsNullOrWhiteSpace(name))
      {
        if (Map.TryGetValue(name, out var glyph))
          return glyph;

        // Try to return the first character as a fallback
        return name.Length > 0 ? name.Substring(0, 1) : Map["Default"];
      }

      return Map["Default"];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
