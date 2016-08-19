// <copyright file="FileNameConverter.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class FileNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 3)
            {
                throw new ArgumentException("The values must contains the input file path, the output file extension and the ouput file template.");
            }

            if (!(values[1] is OutputType))
            {
                return "Invalid output file extension (argument 1).";
            }

            string inputFilePath = values[0] as string;
            OutputType outputFileExtension = (OutputType)values[1];
            string outputFileTemplate = values[2] as string;

            return PathHelpers.GenerateFilePathFromTemplate(inputFilePath, outputFileExtension, outputFileTemplate, 1, 3);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
