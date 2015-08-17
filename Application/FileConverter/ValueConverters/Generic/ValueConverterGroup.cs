// <copyright file="ValueConverterGroup.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Data;

    public class ValueConverterGroup : List<IValueConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
            {
                throw new ArgumentException("The parameter must be a string value.");
            }

            string[] parameters = parameterString.Split('|');
            if (parameters.Length != this.Count)
            {
                throw new ArgumentException("The parameter format must be 'Converter1Parameters|Converter2Parameters|...'.");
            }

            object result = value;
            for (int index = 0; index < this.Count; index++)
            {
                IValueConverter converter = this[index];
                result = converter.Convert(result, targetType, parameters[index], culture);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
