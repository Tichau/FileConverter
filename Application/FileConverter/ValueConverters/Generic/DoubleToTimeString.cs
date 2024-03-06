// <copyright file="DoubleToTimeString.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class DoubleToTimeString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double))
            {
                throw new ArgumentException("The value must be a boolean.");
            }

            double doubleValue = (double)value;
            
            return $"{doubleValue:0.#} second{(doubleValue >= 2 ? "s" : string.Empty)}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
