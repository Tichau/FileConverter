// <copyright file="ConversionJob_Gif.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class ConversionJob_Gif : ConversionJob
    {
        private string intermediateFilePath = string.Empty;
        private ConversionJob pngConversionJob = null;
        private ConversionJob gifConversionJob = null;

        public ConversionJob_Gif(ConversionPreset conversionPreset) : base(conversionPreset)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            string extension = System.IO.Path.GetExtension(this.InputFilePath);
            extension = extension.ToLowerInvariant().Substring(1, extension.Length - 1);

            string inputFilePath = string.Empty;

            // If the output is an image start to convert it into png before send it to ffmpeg.
            if (Helpers.GetExtensionCategory(extension) == Helpers.InputCategoryNames.Image && extension != "png")
            {
                // Generate intermediate file path.
                string fileName = Path.GetFileName(this.OutputFilePath);
                string tempPath = Path.GetTempPath();
                this.intermediateFilePath = PathHelpers.GenerateUniquePath(tempPath + fileName + ".png");

                // Convert input in png file to send it to ffmpeg for the gif conversion.
                ConversionPreset intermediatePreset = new ConversionPreset("To compatible image", OutputType.Png, this.ConversionPreset.InputTypes.ToArray());
                this.pngConversionJob = ConversionJobFactory.Create(intermediatePreset, this.InputFilePath);
                this.pngConversionJob.PrepareConversion(this.InputFilePath, this.intermediateFilePath);

                inputFilePath = this.intermediateFilePath;
            }
            else
            {
                inputFilePath = this.InputFilePath;
            }

            // Convert png file into ico.
            this.gifConversionJob = new ConversionJob_FFMPEG(this.ConversionPreset);
            this.gifConversionJob.PrepareConversion(inputFilePath, this.OutputFilePath);
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            Task updateProgress = this.UpdateProgress();

            if (this.pngConversionJob != null)
            {
                this.UserState = Properties.Resources.ConversionStateReadIntputImage;

                Diagnostics.Debug.Log(string.Empty);
                Diagnostics.Debug.Log("Convert image to PNG (intermediate format).");
                this.pngConversionJob.StartConvertion();

                if (this.pngConversionJob.State != ConversionState.Done)
                {
                    this.ConversionFailed(this.pngConversionJob.ErrorMessage);
                    return;
                }
            }

            Diagnostics.Debug.Log(string.Empty);
            Diagnostics.Debug.Log("Convert png intermediate image to gif.");
            this.gifConversionJob.StartConvertion();

            if (this.gifConversionJob.State != ConversionState.Done)
            {
                this.ConversionFailed(this.gifConversionJob.ErrorMessage);
                return;
            }

            if (!string.IsNullOrEmpty(this.intermediateFilePath))
            {
                Diagnostics.Debug.Log("Delete intermediate file {0}.", this.intermediateFilePath);

                File.Delete(this.intermediateFilePath);
            }

            updateProgress.Wait();
        }

        private async Task UpdateProgress()
        {
            while (this.gifConversionJob.State != ConversionState.Done &&
                   this.gifConversionJob.State != ConversionState.Failed)
            {
                if (this.pngConversionJob != null && this.pngConversionJob.State == ConversionState.InProgress)
                {
                    this.Progress = this.pngConversionJob.Progress;
                }

                if (this.gifConversionJob != null && this.gifConversionJob.State == ConversionState.InProgress)
                {
                    this.Progress = this.gifConversionJob.Progress;
                    this.UserState = this.gifConversionJob.UserState;
                }

                await Task.Delay(40);
            }
        }
    }
}
