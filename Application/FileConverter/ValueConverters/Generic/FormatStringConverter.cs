// <copyright file="FormatStringConverter.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class FormatStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
            {
                throw new ArgumentException("The values must not be null.");
            }
            
            string format = parameter as string;
            if (format == null)
            {
                return string.Empty;
            }

            return string.Format(format, values);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
