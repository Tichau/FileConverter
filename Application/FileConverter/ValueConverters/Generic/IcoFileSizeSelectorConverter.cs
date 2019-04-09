using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FileConverter.ValueConverters.Generic
{
    /// <summary>
    /// Forces the selection of a given size from the ICO file/resource. 
    /// If the exact size does not exists, selects the closest smaller if possible otherwise closest higher resolution.
    /// If no parameter is given, the smallest frame available will be selected
    /// </summary>
    public class IcoFileSizeSelectorConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var size = string.IsNullOrWhiteSpace(parameter?.ToString()) ? 0 : System.Convert.ToInt32(parameter);

            var uri = value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(uri))
            {
                return null;
            }

            if (!uri.StartsWith("pack:"))
            {
                uri = $"pack://application:,,,{uri}";
            }

            var decoder = BitmapDecoder.Create(new Uri(uri), BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);

            var result = decoder.Frames.Where(f => f.Width <= size).OrderByDescending(f => f.Width).FirstOrDefault()
                         ?? decoder.Frames.OrderBy(f => f.Width).FirstOrDefault();

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
