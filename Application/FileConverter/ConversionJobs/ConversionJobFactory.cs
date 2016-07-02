// <copyright file="ConversionJobFactory.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    public static class ConversionJobFactory
    {
        public static ConversionJob Create(ConversionPreset conversionPreset, string inputFilePath)
        {
            string inputFileExtension = System.IO.Path.GetExtension(inputFilePath);
            inputFileExtension = inputFileExtension.ToLowerInvariant().Substring(1, inputFileExtension.Length - 1);
            if (inputFileExtension == "cda")
            {
                return new ConversionJob_ExtractCDA(conversionPreset);    
            }

            if (conversionPreset.OutputType == OutputType.Ico)
            {
                return new ConversionJob_Ico(conversionPreset);
            }

            if (conversionPreset.OutputType == OutputType.Gif)
            {
                return new ConversionJob_Gif(conversionPreset);
            }

            if (Helpers.GetExtensionCategory(inputFileExtension) == Helpers.InputCategoryNames.Image)
            {
                return new ConversionJob_ImageMagick(conversionPreset);
            }

            if (conversionPreset.OutputType == OutputType.Pdf ||
                Helpers.GetExtensionCategory(inputFileExtension) == Helpers.InputCategoryNames.Document)
            {
                return new ConversionJob_Pdf(conversionPreset);
            }

            return new ConversionJob_FFMPEG(conversionPreset);
        }
    }
}
