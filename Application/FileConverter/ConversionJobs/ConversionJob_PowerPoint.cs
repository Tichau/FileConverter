// <copyright file="ConversionJob_PowerPoint.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.Office.Core;
    using PowerPoint = Microsoft.Office.Interop.PowerPoint;

    public class ConversionJob_PowerPoint : ConversionJob
    {
        private PowerPoint.Presentation document;
        private PowerPoint.Application application;

        private string intermediateFilePath = string.Empty;
        private ConversionJob pdf2ImageConversionJob = null;

        public ConversionJob_PowerPoint() : base()
        {
        }

        public ConversionJob_PowerPoint(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        protected override int GetOuputFilesCount()
        {
            if (this.ConversionPreset.OutputType == OutputType.Pdf)
            {
                return 1;
            }

            this.LoadDocumentIfNecessary();

            int pagesCount = this.document.Slides.Count;
            return pagesCount;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.ConversionPreset == null)
            {
                throw new System.Exception("The conversion preset must be valid.");
            }

            // Initialize converters.
            if (this.ConversionPreset.OutputType == OutputType.Pdf)
            {
                this.intermediateFilePath = this.OutputFilePath;
            }
            else
            {
                // Generate intermediate file path.
                string fileName = Path.GetFileNameWithoutExtension(this.InputFilePath);
                string tempPath = Path.GetTempPath();
                this.intermediateFilePath = PathHelpers.GenerateUniquePath(tempPath + fileName + ".pdf");

                ConversionPreset intermediatePreset = new ConversionPreset("Pdf to image", this.ConversionPreset, "pdf");
                this.pdf2ImageConversionJob = ConversionJobFactory.Create(intermediatePreset, this.intermediateFilePath);
                this.pdf2ImageConversionJob.PrepareConversion(this.OutputFilePaths);
            }
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new System.Exception("The conversion preset must be valid.");
            }

            this.UserState = Properties.Resources.ConversionStateReadDocument;

            this.LoadDocumentIfNecessary();

            // Make this document the active document.
            //this.document.Activate();

            this.UserState = Properties.Resources.ConversionStateConversion;

            Diagnostics.Debug.Log("Convert PowerPoint document to pdf.");
            this.document.ExportAsFixedFormat(this.intermediateFilePath, PowerPoint.PpFixedFormatType.ppFixedFormatTypePDF);

            Diagnostics.Debug.Log("Close PowerPoint document '{0}'.", this.InputFilePath);
            this.document.Close();
            this.document = null;

            this.ReleaseApplicationInstanceIfNeeded();
            
            if (this.pdf2ImageConversionJob != null)
            {
                if (!System.IO.File.Exists(this.intermediateFilePath))
                {
                    this.ConversionFailed(Properties.Resources.ErrorCantFindOutputFiles);
                    return;
                }

                Task updateProgress = this.UpdateProgress();

                Diagnostics.Debug.Log("Convert pdf to images.");

                this.pdf2ImageConversionJob.StartConvertion();

                if (this.pdf2ImageConversionJob.State != ConversionState.Done)
                {
                    this.ConversionFailed(this.pdf2ImageConversionJob.ErrorMessage);
                    return;
                }

                if (!string.IsNullOrEmpty(this.intermediateFilePath))
                {
                    Diagnostics.Debug.Log("Delete intermediate file {0}.", this.intermediateFilePath);

                    File.Delete(this.intermediateFilePath);
                }

                updateProgress.Wait();
            }
        }

        private async Task UpdateProgress()
        {
            while (this.pdf2ImageConversionJob.State != ConversionState.Done &&
                   this.pdf2ImageConversionJob.State != ConversionState.Failed)
            {
                if (this.pdf2ImageConversionJob != null && this.pdf2ImageConversionJob.State == ConversionState.InProgress)
                {
                    this.Progress = this.pdf2ImageConversionJob.Progress;
                }

                if (this.pdf2ImageConversionJob != null && this.pdf2ImageConversionJob.State == ConversionState.InProgress)
                {
                    this.Progress = this.pdf2ImageConversionJob.Progress;
                    this.UserState = this.pdf2ImageConversionJob.UserState;
                }

                await Task.Delay(40);
            }
        }

        private void LoadDocumentIfNecessary()
        {
            this.InitializeApplicationInstanceIfNecessary();

            if (this.document == null)
            {
                Diagnostics.Debug.Log("Load PowerPoint document '{0}'.", this.InputFilePath);

                this.document = this.application.Presentations.Open(this.InputFilePath, ReadOnly:MsoTriState.msoTrue, WithWindow:MsoTriState.msoFalse);
            }
        }

        private void InitializeApplicationInstanceIfNecessary()
        {
            if (this.application != null)
            {
                return;
            }

            // Initialize PowerPoint application.
            Diagnostics.Debug.Log("Instantiate PowerPoint application via interop.");
            this.application = new PowerPoint.Application();
        }

        private void ReleaseApplicationInstanceIfNeeded()
        {
            Diagnostics.Debug.Log("Quit PowerPoint application via interop.");
            this.application.Quit();
        }
    }
}
