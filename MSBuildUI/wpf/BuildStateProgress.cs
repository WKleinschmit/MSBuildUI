using System;
using System.Globalization;
using System.Windows.Data;
using MSBuildUI.Items;

namespace MSBuildUI.wpf
{
    public class BuildStateProgress : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BuildState buildState && targetType == typeof(bool))
                return buildState.HasFlag(BuildState.InProgress);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
