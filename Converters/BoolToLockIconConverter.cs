using System.Globalization;

namespace LunaDraw.Converters
{
    public class BoolToLockIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLocked)
            {
                return isLocked ? "🔒" : "🔓";
            }
            return "🔓";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}