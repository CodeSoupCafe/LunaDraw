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
