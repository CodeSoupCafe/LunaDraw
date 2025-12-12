/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

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
