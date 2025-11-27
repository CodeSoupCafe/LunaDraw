using SkiaSharp;
using System.Globalization;

namespace LunaDraw.Converters
{
    public class ColorToHexConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            if (value is SKColor color)
            {
                return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
            }

            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string hexString && !string.IsNullOrWhiteSpace(hexString))
            {
                hexString = hexString.TrimStart('#');

                if (hexString.Length == 6)
                {
                    try
                    {
                        byte r = System.Convert.ToByte(hexString.Substring(0, 2), 16);
                        byte g = System.Convert.ToByte(hexString.Substring(2, 2), 16);
                        byte b = System.Convert.ToByte(hexString.Substring(4, 2), 16);
                        return new SKColor(r, g, b);
                    }
                    catch
                    {
                        // Return a safe default color on conversion failure
                        return SKColors.Black;
                    }
                }
            }

            // Return a safe default color if the input is invalid or empty
            return SKColors.Black;
        }
    }
}
