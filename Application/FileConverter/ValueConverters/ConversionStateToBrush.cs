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

            if (type == "Background")
            {
                switch (conversionState)
                {
                    case ConversionState.Unknown:
                        return new SolidColorBrush(Color.FromRgb(230, 230, 230));

                    case ConversionState.Ready:
                        return new SolidColorBrush(Color.FromRgb(230, 230, 230));

                    case ConversionState.InProgress:
                        return new SolidColorBrush(Color.FromRgb(250, 236, 179));

                    case ConversionState.Done:
                        return new SolidColorBrush(Color.FromRgb(200, 230, 201));

                    case ConversionState.Failed:
                        return new SolidColorBrush(Color.FromRgb(255, 205, 210));
                }
            }
            else if (type == "Foreground")
            {
                switch (conversionState)
                {
                    case ConversionState.Unknown:
                        return new SolidColorBrush(Color.FromRgb(128, 128, 128));

                    case ConversionState.Ready:
                        return new SolidColorBrush(Color.FromRgb(32, 32, 32));

                    case ConversionState.InProgress:
                        return new SolidColorBrush(Color.FromRgb(255, 160, 0));

                    case ConversionState.Done:
                        return new SolidColorBrush(Color.FromRgb(56, 142, 60));

                    case ConversionState.Failed:
                        return new SolidColorBrush(Color.FromRgb(255, 82, 82));
                }
            }
            else
            {
                throw new SystemException("Unknown type.");
            }
            
            throw new SystemException("Unknown state.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
