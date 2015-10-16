// <copyright file="StringToValueType.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class StringToValueType : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string typeName = parameter as string;
            if (typeName == null)
            {
                throw new ArgumentNullException("parameter", "The parameter must contains a convertible type.");
            }

            Type type = Type.GetType(typeName);
            if (type == null)
            {
                throw new Exception("Invalid enum type " + typeName + ".");
            }

            return System.Convert.ChangeType(value, type);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.ToString();
        }
    }
}
