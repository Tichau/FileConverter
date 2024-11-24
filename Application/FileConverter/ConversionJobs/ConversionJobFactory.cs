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
                return new ConversionJob_ExtractCDA(conversionPreset, inputFilePath);    
            }

            if (inputFileExtension == "docx" || inputFileExtension == "odt" || inputFileExtension == "doc")
            {
                return new ConversionJob_Word(conversionPreset, inputFilePath);
            }

            if (inputFileExtension == "xlsx" || inputFileExtension == "ods" || inputFileExtension == "xls")
            {
                return new ConversionJob_Excel(conversionPreset, inputFilePath);
            }

            if (inputFileExtension == "pptx" || inputFileExtension == "odp" || inputFileExtension == "ppt")
            {
                return new ConversionJob_PowerPoint(conversionPreset, inputFilePath);
            }

            if (conversionPreset.OutputType == OutputType.Ico)
            {
                return new ConversionJob_Ico(conversionPreset, inputFilePath);
            }

            if (conversionPreset.OutputType == OutputType.Gif)
            {
                return new ConversionJob_Gif(conversionPreset, inputFilePath);
            }

            if (conversionPreset.OutputType == OutputType.Pdf)
            {
                return new ConversionJob_ImageMagick(conversionPreset, inputFilePath);
            }

            if (conversionPreset.OutputType == OutputType.Jpg ||
                conversionPreset.OutputType == OutputType.Png ||
                conversionPreset.OutputType == OutputType.Webp)
            {
                return new ConversionJob_ImageMagick(conversionPreset, inputFilePath);
            }
            
            return new ConversionJob_FFMPEG(conversionPreset, inputFilePath);
        }
    }
}
