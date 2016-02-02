// <copyright file="ConversionJob_ImageMagick.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using FileConverter.Diagnostics;

namespace FileConverter.ConversionJobs
{
    using System;
    using ImageMagick;

    public class ConversionJob_ImageMagick : ConversionJob
    {
        public ConversionJob_ImageMagick() : base()
        {
        }

        public ConversionJob_ImageMagick(ConversionPreset conversionPreset) : base(conversionPreset)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            using (MagickImage image = new MagickImage(this.InputFilePath))
            {
                Debug.Log("Load image {0} succeed.", this.InputFilePath);

                float scaleFactor = this.ConversionPreset.GetSettingsValue<float>(ConversionPreset.ConversionSettingKeys.ImageScale);
                if (Math.Abs(scaleFactor - 1f) >= 0.005f)
                {
                    Debug.Log("Apply scale factor: {0}%.", scaleFactor * 100);

                    image.Scale(new Percentage(scaleFactor * 100f));
                }

                bool clampSizeToPowerOf2 = this.ConversionPreset.GetSettingsValue<bool>(ConversionPreset.ConversionSettingKeys.ImageClampSizePowerOf2);
                if (clampSizeToPowerOf2)
                {
                    int referenceSize = System.Math.Min(image.Width, image.Height);
                    int size = 2;
                    while (size * 2 < referenceSize)
                    {
                        size *= 2;
                    }

                    Debug.Log("Clamp size to the nearest power of 2 size (from {0}x{1} to {2}x{2}).", image.Width, image.Height, size);

                    image.Scale(size, size);
                }

                int maximumSize = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.ImageMaximumSize);
                if (maximumSize > 0)
                {
                    int width = System.Math.Min(image.Width, maximumSize);
                    int height = System.Math.Min(image.Height, maximumSize);

                    Debug.Log("Clamp size to maximum size of {2}x{2} (from {0}x{1} to {2}x{3}).", image.Width, image.Height, width, height);

                    image.Scale(width, height);
                }

                Debug.Log("Convert image (output: {0}).", this.OutputFilePath);
                switch (this.ConversionPreset.OutputType)
                {
                    case OutputType.Png:
                        image.Quality = 100;
                        break;

                    case OutputType.Jpg:
                        image.Quality = 100;
                        break;

                    default:
                        this.ConversionFailed(string.Format("Unsupported output format {0}.", this.ConversionPreset.OutputType));
                        return;
                }

                image.Write(this.OutputFilePath);
            }
        }
    }
}
