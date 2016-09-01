// <copyright file="ConversionJob_ImageMagick.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;

    using FileConverter.Diagnostics;
    using ImageMagick;

    public class ConversionJob_ImageMagick : ConversionJob
    {
        private bool isInputFilePdf;
        private int pageCount;

        public ConversionJob_ImageMagick() : base()
        {
            this.IsCancelable = true;
        }

        public ConversionJob_ImageMagick(ConversionPreset conversionPreset) : base(conversionPreset)
        {
            this.IsCancelable = true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            string applicationDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            MagickNET.SetGhostscriptDirectory(applicationDirectory);

            this.isInputFilePdf = System.IO.Path.GetExtension(this.InputFilePath).ToLowerInvariant() == ".pdf";

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }
        }

        protected override int GetOuputFilesCount()
        {
            if (System.IO.Path.GetExtension(this.InputFilePath).ToLowerInvariant() == ".pdf")
            {
                using (MagickImageCollection images = new MagickImageCollection())
                {
                    MagickReadSettings settings = new MagickReadSettings();
                    settings.Density = new Density(1, 1);
                    images.Read(this.InputFilePath);

                    return images.Count;
                }
            }

            return 1;
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            this.CurrentOuputFilePathIndex = 0;

            if (this.isInputFilePdf)
            {
                this.ConvertPdf();
            }
            else
            {
                this.pageCount = 1;
                using (MagickImage image = new MagickImage(this.InputFilePath))
                {
                    Debug.Log("Load image {0} succeed.", this.InputFilePath);

                    this.ConvertImage(image);
                }
            }
        }

        private void ConvertPdf()
        {
            MagickReadSettings settings = new MagickReadSettings();

            float dpi = 200f;
            float scaleFactor = this.ConversionPreset.GetSettingsValue<float>(ConversionPreset.ConversionSettingKeys.ImageScale);
            if (Math.Abs(scaleFactor - 1f) >= 0.005f)
            {
                Debug.Log("Apply scale factor: {0}%.", scaleFactor*100);

                dpi *= scaleFactor;
            }

            Debug.Log("Density: {0}dpi.", dpi);
            settings.Density = new Density(dpi, dpi);

            this.UserState = Properties.Resources.ConversionStateReadDocument;

            using (MagickImageCollection images = new MagickImageCollection())
            {
                // Add all the pages of the pdf file to the collection
                images.Read(this.InputFilePath, settings);
                Debug.Log("Load pdf {0} succeed.", this.InputFilePath);

                this.pageCount = images.Count;

                this.UserState = Properties.Resources.ConversionStateConversion;

                foreach (MagickImage image in images)
                {
                    Debug.Log("Write page {0}/{1}.", this.CurrentOuputFilePathIndex + 1, this.pageCount);
                    this.ConvertImage(image, true);

                    this.CurrentOuputFilePathIndex++;
                }
            }
        }

        private void ConvertImage(MagickImage image, bool ignoreScale = false)
        {
            image.Progress += this.Image_Progress;

            if (!ignoreScale && this.ConversionPreset.IsRelevantSetting(ConversionPreset.ConversionSettingKeys.ImageScale))
            {
                float scaleFactor = this.ConversionPreset.GetSettingsValue<float>(ConversionPreset.ConversionSettingKeys.ImageScale);
                if (Math.Abs(scaleFactor - 1f) >= 0.005f)
                {
                    Debug.Log("Apply scale factor: {0}%.", scaleFactor*100);

                    image.Scale(new Percentage(scaleFactor*100f));
                }
            }

            if (this.ConversionPreset.IsRelevantSetting(ConversionPreset.ConversionSettingKeys.ImageRotation))
            {
                float rotateAngleInDegrees = this.ConversionPreset.GetSettingsValue<float>(ConversionPreset.ConversionSettingKeys.ImageRotation);
                if (Math.Abs(rotateAngleInDegrees - 0f) >= 0.05f)
                {
                    Debug.Log("Apply rotation: {0}°.", rotateAngleInDegrees);

                    image.Rotate(rotateAngleInDegrees);
                }
            }

            if (this.ConversionPreset.IsRelevantSetting(ConversionPreset.ConversionSettingKeys.ImageClampSizePowerOf2))
            {
                bool clampSizeToPowerOf2 = this.ConversionPreset.GetSettingsValue<bool>(ConversionPreset.ConversionSettingKeys.ImageClampSizePowerOf2);
                if (clampSizeToPowerOf2)
                {
                    int referenceSize = System.Math.Min(image.Width, image.Height);
                    int size = 2;
                    while (size*2 <= referenceSize)
                    {
                        size *= 2;
                    }

                    Debug.Log("Clamp size to the nearest power of 2 size (from {0}x{1} to {2}x{2}).", image.Width, image.Height, size);

                    image.Scale(size, size);
                }
            }

            if (this.ConversionPreset.IsRelevantSetting(ConversionPreset.ConversionSettingKeys.ImageMaximumSize))
            {
                int maximumSize = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.ImageMaximumSize);
                if (maximumSize > 0)
                {
                    int width = System.Math.Min(image.Width, maximumSize);
                    int height = System.Math.Min(image.Height, maximumSize);

                    Debug.Log("Clamp size to maximum size of {2}x{2} (from {0}x{1} to {2}x{3}).", image.Width, image.Height, width, height);

                    image.Scale(width, height);
                }
            }

            Debug.Log("Convert image (output: {0}).", this.OutputFilePath);
            switch (this.ConversionPreset.OutputType)
            {
                case OutputType.Png:
                    image.Quality = 100;
                    break;

                case OutputType.Jpg:
                    image.Quality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.ImageQuality);
                    break;

                case OutputType.Pdf:
                    break;

                default:
                    this.ConversionFailed(string.Format(Properties.Resources.ErrorUnsupportedOutputFormat, this.ConversionPreset.OutputType));
                    image.Progress -= this.Image_Progress;
                    return;
            }

            image.Write(this.OutputFilePath);
            image.Progress -= this.Image_Progress;
        }

        private void Image_Progress(object sender, ProgressEventArgs eventArgs)
        {
            if (this.CancelIsRequested)
            {
                eventArgs.Cancel = true;
                return;
            }

            float alreadyCompletedPages = this.CurrentOuputFilePathIndex / (float)this.pageCount;
            this.Progress = alreadyCompletedPages + (float)eventArgs.Progress.ToDouble() / (100f * this.pageCount);
        }
    }
}
