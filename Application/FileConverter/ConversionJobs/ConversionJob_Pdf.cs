// <copyright file="ConversionJob_ImageMagick.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;

    using FileConverter.Diagnostics;
    using ImageMagick;

    public class ConversionJob_Pdf : ConversionJob
    {
        private int pageCount = 0;

        public ConversionJob_Pdf() : base()
        {
        }

        public ConversionJob_Pdf(ConversionPreset conversionPreset) : base(conversionPreset)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            string applicationDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            MagickNET.SetGhostscriptDirectory(applicationDirectory);

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }
        }

        protected override int GetOuputFilesCount()
        {
            using (MagickImageCollection images = new MagickImageCollection())
            {
                MagickReadSettings settings = new MagickReadSettings();
                settings.Density = new Density(1, 1);
                images.Read(this.InputFilePath);

                return images.Count;
            }
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            this.UserState = Properties.Resources.ConversionStateReadPdf;

            MagickReadSettings settings = new MagickReadSettings();
            // Settings the density to 300 dpi will create an image with a better quality
            settings.Density = new Density(300, 300);

            using (MagickImageCollection images = new MagickImageCollection())
            {
                // Add all the pages of the pdf file to the collection
                images.Read(this.InputFilePath, settings);
                Debug.Log("Load pdf {0} succeed.", this.InputFilePath);

                this.pageCount = images.Count;

                this.CurrentOuputFilePathIndex = 0;
                
                foreach (MagickImage image in images)
                {
                    this.UserState = Properties.Resources.ConversionStateConversion;

                    Debug.Log("Write page {0}/{1}.", this.CurrentOuputFilePathIndex + 1, this.pageCount);

                    image.Progress += this.Image_Progress;
                    
                    // Write page to file that contains the page number
                    image.Write(this.OutputFilePath);
                    this.CurrentOuputFilePathIndex++;

                    image.Progress -= this.Image_Progress;
                }
            }
        }

        private void Image_Progress(object sender, ProgressEventArgs eventArgs)
        {
            float alreadyCompletedPages = this.CurrentOuputFilePathIndex / (float)this.pageCount;
            this.Progress = alreadyCompletedPages + (float)eventArgs.Progress.ToDouble() / (100f * this.pageCount);
        }
    }
}
