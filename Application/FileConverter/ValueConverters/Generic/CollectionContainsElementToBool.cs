// <copyright file="CollectionContainsElementToBool.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Data;

    public class CollectionContainsElementToBool : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
            {
                throw new ArgumentException("The values must contains the collection of elements and the researched element.");
            }

            //// TODO: Make this converter generic.
            
            if (!(values[0] is ICollection<string>))
            {
                return false;
            }

            bool? result = null;
            if (values[1] is IEnumerable<string>)
            {
                ICollection<string> collection = values[0] as ICollection<string>;
                IEnumerable<string> objectsToFind = values[1] as IEnumerable<string>;
                
                bool all = true;
                bool none = true;
                foreach (string objectToFind in objectsToFind)
                {
                    bool contains = collection.Contains(objectToFind);
                    all &= contains;
                    none &= !contains;
                }

                if (all)
                {
                    result = true;
                }
                else if (none)
                {
                    result = false;
                }
            }
            else
            {
                ICollection<string> collection = values[0] as ICollection<string>;
                string objectToFind = values[1] as string;

                result = collection.Contains(objectToFind);
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
