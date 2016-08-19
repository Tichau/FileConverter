// <copyright file="BitrateToString.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Linq;
    using FileConverter.Windows;

    public class OutputTypeEnumToViewModel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is OutputType))
            {
                throw new ArgumentException("The value must be an output type value.");
            }

            OutputType outputType = (OutputType)value;

            return SettingsWindow.OutputTypeViewModels.FirstOrDefault(match => match.Type == outputType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            OutputTypeViewModel outputTypeViewModel = value as OutputTypeViewModel;
            if (outputTypeViewModel == null)
            {
                return OutputType.None;
            }

            return outputTypeViewModel.Type;
        }
    }
}