using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MSBuildUI.wpf
{
    public class BoolToBorderStyle : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && targetType == typeof(Style))
                return Application.Current.FindResource(boolValue ? "ActiveBorder" : "InactiveBorder");
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
