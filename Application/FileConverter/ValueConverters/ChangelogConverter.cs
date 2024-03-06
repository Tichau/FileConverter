// <copyright file="ChangelogConverter.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using FileConverter.Diagnostics;

    public class ChangelogConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool header = false;
            if (parameter != null)
            {
                switch (parameter)
                {
                    case string s:
                        {
                            if (!bool.TryParse(s, out header))
                            {
                                Debug.LogError($"Invalid parameter {s}");
                            }

                            break;
                        }

                    case bool b:
                        header = b;
                        break;

                    default:
                        Debug.LogError($"Invalid parameter {parameter}");
                        break;
                }
            }

            string content = string.Empty;
            if (header)
            {
                content += Properties.Resources.LicenceHeader1 + "\n";
                content += Properties.Resources.LicenceHeader2 + "\n\n";
                content += Properties.Resources.LicenceHeader3 + "\n\n";
            }

            string changelog = (string)value;
            content += changelog;

            return content;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
