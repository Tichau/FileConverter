// <copyright file="InputTypesToBool.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Data;

    public class InputTypesToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<string> intputTypes = value as List<string>;
            if (intputTypes == null)
            {
                throw new ArgumentException("The value must be an list of string.");
            }

            string referenceTypeName = parameter as string;
            if (string.IsNullOrEmpty(referenceTypeName))
            {
                return false;
            }

            return intputTypes.Contains(referenceTypeName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
