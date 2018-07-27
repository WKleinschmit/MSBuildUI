using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MSBuildUI.Items;

namespace MSBuildUI.wpf
{
    public class BuildStateColor : IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BuildState buildState && targetType == typeof(Brush))
            {
                switch (buildState & BuildState.ColorMask)
                {
                    case BuildState.Inactive:
                        return Brushes.Gray;
                    case BuildState.Waiting:
                        return Brushes.DodgerBlue;
                    case BuildState.Success:
                        return Brushes.Lime;
                    case BuildState.Warning:
                        return Brushes.Yellow;
                    case BuildState.Error:
                        return Brushes.Red;
                }
            }

            return null;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            switch (values.Length)
            {
                case 2 when values[0] is bool isActive && values[1] is BuildState buildState && targetType == typeof(Brush):
                    return isActive ? Convert(values[1], targetType, parameter, culture) : Brushes.Gray;
                case 1:
                    return Convert(values[0], targetType, parameter, culture);
                default:
                    return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
