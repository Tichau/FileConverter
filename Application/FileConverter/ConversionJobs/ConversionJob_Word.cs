// <copyright file="ConversionJob_Word.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System.IO;
    using System.Threading.Tasks;

    using Word = Microsoft.Office.Interop.Word;

    public class ConversionJob_Word : ConversionJob_Office
    {
        private Word.Document document;
        private Word.Application wordApplication;

        private string intermediateFilePath = string.Empty;
        private ConversionJob pdf2ImageConversionJob = null;

        public ConversionJob_Word() : base()
        {
        }

        public ConversionJob_Word(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        protected override int GetOuputFilesCount()
        {
            if (this.ConversionPreset.OutputType == OutputType.Pdf)
            {
                return 1;
            }

            this.LoadDocumentIfNecessary();

            int pagesCount = this.document.ComputeStatistics(Word.WdStatistic.wdStatisticPages);

            return pagesCount;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.State == ConversionState.Failed)
            {
                return;
            }

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
            this.document.Activate();

            this.UserState = Properties.Resources.ConversionStateConversion;

            Diagnostics.Debug.Log("Convert word document to pdf.");
            this.document.ExportAsFixedFormat(this.intermediateFilePath, Word.WdExportFormat.wdExportFormatPDF);

            Diagnostics.Debug.Log("Close word document '{0}'.", this.InputFilePath);
            this.document.Close(Word.WdSaveOptions.wdDoNotSaveChanges);
            this.document = null;

            this.ReleaseOfficeApplicationInstanceIfNeeded();
            
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

        protected override void InitializeOfficeApplicationInstanceIfNecessary()
        {
            if (this.wordApplication != null)
            {
                return;
            }

            // Initialize word application.
            Diagnostics.Debug.Log("Instantiate word application via interop.");
            this.wordApplication = new Microsoft.Office.Interop.Word.Application
            {
                Visible = false
            };
        }

        protected override void ReleaseOfficeApplicationInstanceIfNeeded()
        {
            Diagnostics.Debug.Log("Quit word application via interop.");
            this.wordApplication.Quit();
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
            this.InitializeOfficeApplicationInstanceIfNecessary();

            if (this.document == null)
            {
                Diagnostics.Debug.Log("Load word document '{0}'.", this.InputFilePath);

                this.document = this.wordApplication.Documents.Open(this.InputFilePath, System.Reflection.Missing.Value, true);
            }
        }
    }
}
