// <copyright file="BoolToVisibility.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class BoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && !(value is bool))
            {
                throw new ArgumentException("The value must be a boolean.");
            }

            bool booleanValue = (bool?) value ?? false;

            string stringParameter = parameter as string;

            string trueResult = "Visible";
            string falseResult = "Hidden";
            if (!string.IsNullOrEmpty(stringParameter))
            {
                string[] results = stringParameter.Split(';');
                Diagnostics.Debug.Assert(results.Length <= 2, "results.Length <= 2");

                if (results.Length >= 1)
                {
                    falseResult = results[0];
                }

                if (results.Length >= 2)
                {
                    trueResult = results[1];
                }
            }


            return booleanValue ? trueResult : falseResult;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}