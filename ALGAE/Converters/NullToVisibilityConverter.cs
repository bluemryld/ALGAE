using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ALGAE
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null || string.IsNullOrWhiteSpace(value.ToString());
            
            // Check if parameter specifies "Collapsed" behavior (default is Hidden)
            string? param = parameter?.ToString()?.ToLower();
            Visibility nullVisibility = param == "collapsed" ? Visibility.Collapsed : Visibility.Hidden;
            
            return isNull ? nullVisibility : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
