using System.Globalization;
using System.Windows.Data;

namespace FifaAttribDbAppPlugin.WPF
{
    public class ObjectToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            if (value is float f)
                return f.ToString(culture);
            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return null;
            return text;
        }
    }
}
