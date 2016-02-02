// <copyright file="ConversionJob_Ico.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;

    public class ConversionJob_Ico : ConversionJob
    {
        private string intermediateFilePath;
        private ConversionJob pngConversionJob;
        private ConversionJob icoConversionJob;

        public ConversionJob_Ico(ConversionPreset conversionPreset) : base(conversionPreset)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            // Generate intermediate file path.
            string fileName = Path.GetFileName(this.OutputFilePath);
            string tempPath = Path.GetTempPath();
            this.intermediateFilePath = PathHelpers.GenerateUniquePath(tempPath + fileName + ".png");

            // Convert input in png file to send it to ffmpeg for the ico conversion.
            ConversionPreset intermediatePreset = new ConversionPreset("To compatible image", OutputType.Png, this.ConversionPreset.InputTypes.ToArray());
            intermediatePreset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.ImageClampSizePowerOf2, "True");
            intermediatePreset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.ImageMaximumSize, "256");
            this.pngConversionJob = ConversionJobFactory.Create(intermediatePreset, this.InputFilePath);
            this.pngConversionJob.PrepareConversion(this.InputFilePath, this.intermediateFilePath);

            // Convert png file into ico.
            this.icoConversionJob = new ConversionJob_FFMPEG(this.ConversionPreset);
            this.icoConversionJob.PrepareConversion(this.intermediateFilePath, this.OutputFilePath);
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            Diagnostics.Debug.Log(string.Empty);
            Diagnostics.Debug.Log("Convert image to PNG (intermediate format).");
            this.pngConversionJob.StartConvertion();

            if (this.pngConversionJob.State != ConversionState.Done)
            {
                this.ConversionFailed(this.pngConversionJob.ErrorMessage);
                return;
            }

            Diagnostics.Debug.Log(string.Empty);
            Diagnostics.Debug.Log("Convert png intermediate image to ICO.");
            this.icoConversionJob.StartConvertion();

            if (this.icoConversionJob.State != ConversionState.Done)
            {
                this.ConversionFailed(this.icoConversionJob.ErrorMessage);
                return;
            }

            Diagnostics.Debug.Log("Delete intermediate file {0}.", this.intermediateFilePath);

            File.Delete(this.intermediateFilePath);
        }
    }
}
