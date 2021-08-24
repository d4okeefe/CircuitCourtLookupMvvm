using System;
using System.Globalization;
using System.Windows.Data;

namespace CircuitCourtLookupMvvm.Converters
{
    class GridHeightCalculator : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
            return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
