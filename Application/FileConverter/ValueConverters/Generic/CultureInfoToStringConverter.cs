// <copyright file="CultureInfoToStringConverter.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class CultureInfoToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not CultureInfo cultureInfo)
            {
                throw new System.ArgumentException("The object must be a culture info instance.");
            }

            string nativeName = cultureInfo.NativeName;
            if (string.IsNullOrEmpty(nativeName))
            {
                return cultureInfo.Name;
            }

            if (nativeName.Length == 1)
            {
                return nativeName.ToUpper(cultureInfo);
            }

            return nativeName.Substring(0, 1).ToUpper(cultureInfo) + nativeName.Substring(1, nativeName.Length - 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
