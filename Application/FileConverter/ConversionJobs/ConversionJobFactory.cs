// <copyright file="ConversionJobFactory.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    public static class ConversionJobFactory
    {
        public static ConversionJob Create(ConversionPreset conversionPreset, string inputFilePath)
        {
            ConversionJob conversionJob = null;

            string extension = System.IO.Path.GetExtension(inputFilePath);
            extension = extension.ToLowerInvariant().Substring(1, extension.Length - 1);
            if (extension == "cda")
            {
                conversionJob = new ConversionJob_ExtractCDA(conversionPreset);    
            }
            else
            {
                conversionJob = new ConversionJob_FFMPEG(conversionPreset);
            }

            return conversionJob;
        }
    }
}
