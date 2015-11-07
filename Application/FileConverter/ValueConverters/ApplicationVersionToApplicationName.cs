// <copyright file="ApplicationVersionToApplicationName.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class ApplicationVersionToApplicationName : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is FileConverter.Version))
            {
                return "File Converter";
            }

            FileConverter.Version version = (FileConverter.Version)value;

            return string.Format("File Converter v{0}", version.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
