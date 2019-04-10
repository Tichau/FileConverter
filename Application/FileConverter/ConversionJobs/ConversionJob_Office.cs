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

        public enum ApplicationName
        {
            None,

            Word,
            Excel,
            PowerPoint
        }

        protected abstract ApplicationName Application
        {
            get;
        }

        protected override bool IsCancelable()
        {
            return false;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!Helpers.IsMicrosoftOfficeApplicationAvailable(this.Application))
            {
                switch (this.Application)
                {
                    case ApplicationName.Word:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftWordIsNotAvailable);
                        return;

                    case ApplicationName.PowerPoint:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftPowerPointIsNotAvailable);
                        return;

                    case ApplicationName.Excel:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftExcelIsNotAvailable);
                        return;

                    default:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftOfficeIsNotAvailable);
                        return;
                }
            }
        }

        protected override void OnConversionFailed()
        {
            base.OnConversionFailed();

            this.ReleaseOfficeApplicationInstanceIfNeeded();
        }

        protected abstract void InitializeOfficeApplicationInstanceIfNecessary();

        protected abstract void ReleaseOfficeApplicationInstanceIfNeeded();
    }
}