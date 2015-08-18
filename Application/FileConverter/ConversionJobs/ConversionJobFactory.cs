// <copyright file="ConversionJobFactory.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    public static class ConversionJobFactory
    {
        public static ConversionJob Create(ConversionPreset conversionPreset)
        {
            ConversionJob conversionJob = new ConversionJob_FFMPEG(conversionPreset);

            return conversionJob;
        }
    }
}
