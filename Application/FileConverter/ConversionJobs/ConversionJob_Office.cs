// <copyright file="ConversionJob_Office.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    public abstract class ConversionJob_Office : ConversionJob
    {
        protected ConversionJob_Office() : base()
        {
        }

        protected ConversionJob_Office(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!Helpers.IsMicrosoftOfficeAvailable())
            {
                this.ConversionFailed(Properties.Resources.ErrorMicrosoftOfficeIsNotAvailable);
                return;
            }
        }
    }
}