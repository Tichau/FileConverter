// <copyright file="ObjectToLocalizedString.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class ObjectToLocalizedString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                throw new System.ArgumentNullException(nameof(value));
            }

            string stringValue = value.ToString();
            string typeName = value.GetType().Name;

            string localizationKey = $"{typeName}{stringValue}Name";
            localizationKey = localizationKey.Replace(" ", string.Empty);

            string resource = Properties.Resources.ResourceManager.GetString(localizationKey);
            if (resource == null)
            {
                return localizationKey;
            }

            return resource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
