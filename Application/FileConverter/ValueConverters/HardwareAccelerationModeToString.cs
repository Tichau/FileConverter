// <copyright file="HardwareAccelerationModeToString.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class HardwareAccelerationModeToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value) {
                case Helpers.HardwareAccelerationMode.Off:
                    return "Off";
                case Helpers.HardwareAccelerationMode.CUDA:
                    return "Nvidia (CUDA)";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}