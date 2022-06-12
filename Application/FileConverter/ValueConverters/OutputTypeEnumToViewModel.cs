// <copyright file="OutputTypeEnumToViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;

    using CommonServiceLocator;

    using FileConverter.ViewModels;

    public class OutputTypeEnumToViewModel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is OutputType))
            {
                throw new ArgumentException("The value must be an output type value.");
            }

            OutputType outputType = (OutputType)value;

            SettingsViewModel settingsViewModel = ServiceLocator.Current.GetInstance<SettingsViewModel>();

            return settingsViewModel.OutputTypes.Cast<OutputTypeViewModel>()
                .FirstOrDefault(match => match.Type == outputType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not OutputTypeViewModel outputTypeViewModel)
            {
                return OutputType.None;
            }

            return outputTypeViewModel.Type;
        }
    }
}