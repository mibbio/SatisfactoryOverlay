namespace SatisfactoryOverlay.Converter
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    [ValueConversion(typeof(bool), typeof(bool))]
    internal class BooleanInversionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolValue) ? !boolValue : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolValue) ? !boolValue : value;
        }
    }
}
