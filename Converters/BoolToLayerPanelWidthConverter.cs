using System.Globalization;

namespace LunaDraw.Converters
{
    public class BoolToLayerPanelWidthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? 300.0 : 120.0;
            }

            return 300.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return 300.0;
        }
    }
}
