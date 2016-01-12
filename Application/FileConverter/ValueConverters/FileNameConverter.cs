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

            if (inputFilePath == null)
            {
                return "Invalid input file path (argument 0).";
            }
            
            string inputExtension = System.IO.Path.GetExtension(inputFilePath).Substring(1);
            string inputPathWithoutExtension = inputFilePath.Substring(0, inputFilePath.Length - inputExtension.Length - 1);
            string outputExtension = outputFileExtension.ToString().ToLowerInvariant();

            if (outputFileTemplate == null)
            {
                // Default output path.
                return inputPathWithoutExtension + "." + outputExtension;
            }

            string fileName = System.IO.Path.GetFileName(inputPathWithoutExtension);
            string parentDirectory = System.IO.Path.GetDirectoryName(inputPathWithoutExtension);
            string[] directories = parentDirectory.Split(System.IO.Path.DirectorySeparatorChar);
            parentDirectory += System.IO.Path.DirectorySeparatorChar;

            // Generate output path from template.
            string outputPath = outputFileTemplate;

            outputPath = outputPath.Replace("(path)", parentDirectory);
            outputPath = outputPath.Replace("(p)", parentDirectory);

            outputPath = outputPath.Replace("(filename)", fileName);
            outputPath = outputPath.Replace("(f)", fileName);
            outputPath = outputPath.Replace("(F)", fileName.ToUpperInvariant());

            outputPath = outputPath.Replace("(outputext)", outputExtension);
            outputPath = outputPath.Replace("(o)", outputExtension);
            outputPath = outputPath.Replace("(O)", outputExtension.ToUpperInvariant());

            outputPath = outputPath.Replace("(inputext)", inputExtension);
            outputPath = outputPath.Replace("(i)", inputExtension);
            outputPath = outputPath.Replace("(I)", inputExtension.ToUpperInvariant());

            string myDocumentsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "\\";
            outputPath = outputPath.Replace("(p:d)", myDocumentsFolder);
            outputPath = outputPath.Replace("(p:documents)", myDocumentsFolder);

            string myMusicFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic) + "\\";
            outputPath = outputPath.Replace("(p:m)", myMusicFolder);
            outputPath = outputPath.Replace("(p:music)", myMusicFolder);

            string myVideoFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos) + "\\";
            outputPath = outputPath.Replace("(p:v)", myVideoFolder);
            outputPath = outputPath.Replace("(p:videos)", myVideoFolder);

            string myPictureFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures) + "\\";
            outputPath = outputPath.Replace("(p:p)", myPictureFolder);
            outputPath = outputPath.Replace("(p:pictures)", myPictureFolder);

            for (int index = 0; index < directories.Length; index++)
            {
                outputPath = outputPath.Replace(string.Format("(d{0})", directories.Length - index - 1), directories[index]);
                outputPath = outputPath.Replace(string.Format("(D{0})", directories.Length - index - 1), directories[index].ToUpperInvariant());
            }

            outputPath += "." + outputExtension;
            
            return outputPath;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
