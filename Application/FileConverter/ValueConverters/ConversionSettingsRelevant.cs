// <copyright file="ConversionSettingsToString.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class ConversionSettingsRelevant : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (!(value is IConversionSettings))
            {
                throw new ArgumentException("The value must be a conversion preset array.");
            }

            IConversionSettings settings = (IConversionSettings)value;

            string key = parameter as string;
            if (key == null)
            {
                throw new ArgumentException("The parameter must be a string value.");
            }

            return settings.ContainsKey(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new Exception();
        }
    }
}
