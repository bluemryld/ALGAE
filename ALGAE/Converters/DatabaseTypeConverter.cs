using System;
using System.Globalization;
using System.Windows.Data;

namespace ALGAE
{
    public class DatabaseTypeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return "Unknown";
            
            var isDebugDatabase = values[0] is bool debug && debug;
            var isDefault = values[1] is bool defaultDb && defaultDb;
            
            if (isDebugDatabase)
                return "Debug";
            else if (isDefault)
                return "Default";
            else
                return "Custom";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}