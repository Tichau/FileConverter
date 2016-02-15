// <copyright file="ConversionJobFactory.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    public static class ConversionJobFactory
    {
        public static ConversionJob Create(ConversionPreset conversionPreset, string inputFilePath)
        {
            string extension = System.IO.Path.GetExtension(inputFilePath);
            extension = extension.ToLowerInvariant().Substring(1, extension.Length - 1);
            if (extension == "cda")
            {
                return new ConversionJob_ExtractCDA(conversionPreset);    
            }

            if (conversionPreset.OutputType == OutputType.Ico)
            {
                return new ConversionJob_Ico(conversionPreset);
            }

            if (PathHelpers.GetExtensionCategory(extension) == PathHelpers.InputCategoryNames.Image)
            {
                return new ConversionJob_ImageMagick(conversionPreset);
            }

            return new ConversionJob_FFMPEG(conversionPreset);
        }
    }
}
