// <copyright file="InputTypesToBool.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class ConversionSettingsToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (!(value is ConversionPreset))
            {
                throw new ArgumentException("The value must be a conversion preset array.");
            }

            ConversionPreset preset = (ConversionPreset)value;

            string parameterString = parameter as string;
            if (parameterString == null)
            {
                throw new ArgumentException("The parameter must be a string value.");
            }

            string[] parameters = parameterString.Split(',');
            if (parameters.Length != 1)
            {
                throw new ArgumentException("The parameter format must be 'SettingsKey'.");
            }

            string key = parameters[0];
            return preset.GetSettingsValue(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}