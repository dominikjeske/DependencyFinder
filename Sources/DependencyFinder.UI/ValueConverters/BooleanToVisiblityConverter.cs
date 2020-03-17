using System;
using System.Windows;
using System.Windows.Data;

namespace DependencyFinder.UI.ValueConverters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisiblityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ee)
            {

                throw;
            }

            
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }
}