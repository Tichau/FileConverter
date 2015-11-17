// <copyright file="IsOutputTypeCompatibleWithCategoryConverter.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class IsOutputTypeCompatibleWithCategoryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
            {
                throw new ArgumentException("The values must contains the collection of elements and the researched element.");
            }

            if (!(values[0] is OutputType))
            {
                return false;
            }

            OutputType outputType = (OutputType)values[0];

            string category = null;

            string input = values[1] as string;
            if (input == null)
            {
                return false;
            }

            string stringParameter = parameter as string;
            if (stringParameter != null && stringParameter == "InputType")
            {
                category = PathHelpers.GetExtensionCategory(input);
            }
            else if (stringParameter != null && stringParameter == "Category")
            {
                category = input;
            }

            if (category == null)
            {
                return false;
            }

            return PathHelpers.IsOutputTypeCompatibleWithCategory(outputType, category);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
