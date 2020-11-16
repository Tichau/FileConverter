// <copyright file="ConversionStateToBrush.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using FileConverter.ConversionJobs;

    public class ConversionStateToBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ConversionState))
            {
                throw new ArgumentException("The value must be a conversion state.");
            }

            ConversionState conversionState = (ConversionState)value;
            string type = parameter as string;

            return Application.Current.Resources[$"{conversionState}{type}Brush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
