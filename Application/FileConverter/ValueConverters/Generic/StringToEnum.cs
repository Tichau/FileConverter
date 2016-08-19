// <copyright file="StringToEnum.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class StringToEnum : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value as string;
            if (value == null)
            {
                // TODO default value ?
                return null;
            }

            string typeName = parameter as string;
            if (typeName == null)
            {
                throw new ArgumentNullException("parameter", "The parameter must contains the enum type.");
            }

            Type enumType = Type.GetType(typeName);
            if (enumType == null)
            {
                throw new Exception("Invalid enum type " + typeName + ".");
            }

            return Enum.Parse(enumType, stringValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString();
        }
    }
}
