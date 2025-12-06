using System.Globalization;
using LunaDraw.Logic.Models;

namespace LunaDraw.Converters
{
    public class BoolToEyeIconConverter : IValueConverter
    {
        public object Convert(object ?value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                return isVisible ? "👁" : "○"; // Eye vs Empty Circle
            }
            return "○";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string str && str == "👁";
        }
    }
}