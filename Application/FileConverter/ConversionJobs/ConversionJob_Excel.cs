// <copyright file="ConversionJob_Excel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System.IO;
    using System.Threading.Tasks;

    using Excel = Microsoft.Office.Interop.Excel;

    public class ConversionJob_Excel : ConversionJob
    {
        private Excel.Workbook document;
        private Excel.Application application;

        private string intermediateFilePath = string.Empty;
        private ConversionJob pdf2ImageConversionJob = null;

        public ConversionJob_Excel() : base()
        {
        }

        public ConversionJob_Excel(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        protected override int GetOuputFilesCount()
        {
            if (this.ConversionPreset.OutputType == OutputType.Pdf)
            {
                return 1;
            }

            this.LoadDocumentIfNecessary();

            int pagesCount = 0;
            foreach (object sheet in this.document.Sheets)
            {
                Excel.Worksheet worksheet = sheet as Excel.Worksheet;
                if (worksheet != null)
                {
                    pagesCount = worksheet.PageSetup.Pages.Count;
                }
            }

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
            this.document.Activate();

            this.UserState = Properties.Resources.ConversionStateConversion;

            Diagnostics.Debug.Log("Convert excel document to pdf.");
            this.document.ExportAsFixedFormat(Excel.XlFixedFormatType.xlTypePDF, this.intermediateFilePath);

            Diagnostics.Debug.Log("Close excel document '{0}'.", this.InputFilePath);
            this.document.Close(false);
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
                Diagnostics.Debug.Log("Load excel document '{0}'.", this.InputFilePath);

                this.document = this.application.Workbooks.Open(this.InputFilePath, System.Reflection.Missing.Value, true);
            }
        }

        private void InitializeApplicationInstanceIfNecessary()
        {
            if (this.application != null)
            {
                return;
            }

            // Initialize excel application.
            Diagnostics.Debug.Log("Instantiate excel application via interop.");
            this.application = new Excel.Application
            {
                Visible = false
            };
        }

        private void ReleaseApplicationInstanceIfNeeded()
        {
            Diagnostics.Debug.Log("Quit excel application via interop.");
            this.application.Quit();
        }
    }
}
