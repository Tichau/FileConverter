// <copyright file="CollectionContainsElementToBool.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FileConverter.ValueConverters.Generic
{
    public class CollectionContainsElementToBool : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
            {
                throw new ArgumentException("The values must contains the collection of elements and the researched element.");
            }

            // TODO: Make this converter generic.

            if (!(values[0] is ICollection<string>))
            {
                return false;
            }

            ICollection<string> collection = values[0] as ICollection<string>;
            string objectToFind = values[1] as string;

            return collection.Contains(objectToFind);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
