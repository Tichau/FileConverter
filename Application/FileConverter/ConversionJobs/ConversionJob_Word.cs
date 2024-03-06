// <copyright file="ConversionJob_Word.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using FileConverter.Diagnostics;

    using Word = Microsoft.Office.Interop.Word;

    public class ConversionJob_Word : ConversionJob_Office
    {
        private Word.Document document;
        private Word.Application application;

        private string intermediateFilePath = string.Empty;
        private ConversionJob pdf2ImageConversionJob = null;

        public ConversionJob_Word() : base()
        {
        }

        public ConversionJob_Word(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        protected override ApplicationName Application => ApplicationName.Word;

        protected override bool IsCancelable() => false;

        protected override int GetOutputFilesCount()
        {
            if (this.ConversionPreset.OutputType == OutputType.Pdf)
            {
                return 1;
            }

            if (!this.TryLoadDocumentIfNecessary())
            {
                return 1;
            }

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

            if (!this.TryLoadDocumentIfNecessary())
            {
                this.ConversionFailed(Properties.Resources.ErrorUnableToUseMicrosoftOffice);
                return;
            }

            // Make this document the active document.
            this.document.Activate();

            this.UserState = Properties.Resources.ConversionStateConversion;

            Diagnostics.Debug.Log("Convert word document to pdf.");
            // this.document.ExportAsFixedFormat(this.intermediateFilePath, Word.WdExportFormat.wdExportFormatPDF);
            this.document.ExportAsFixedFormat(this.intermediateFilePath, 
                Word.WdExportFormat.wdExportFormatPDF, 
                false, 
                Word.WdExportOptimizeFor.wdExportOptimizeForPrint, 
                Word.WdExportRange.wdExportAllDocument, 
                1, 1, 
                Word.WdExportItem.wdExportDocumentContent, 
                true, 
                true, 
                Word.WdExportCreateBookmarks.wdExportCreateHeadingBookmarks, 
                true);

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

                this.pdf2ImageConversionJob.StartConversion();

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
            if (this.application != null)
            {
                return;
            }

            // Initialize word application.
            Diagnostics.Debug.Log("Instantiate word application via interop.");
            this.application = new Microsoft.Office.Interop.Word.Application
            {
                Visible = false
            };
        }

        protected override void ReleaseOfficeApplicationInstanceIfNeeded()
        {
            if (this.application == null)
            {
                return;
            }

            Diagnostics.Debug.Log("Quit word application via interop.");
            this.application.Quit();
            this.application = null;
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

        private bool TryLoadDocumentIfNecessary()
        {
            try
            {
                this.InitializeOfficeApplicationInstanceIfNecessary();
            }
            catch (Exception exception)
            {
                Debug.Log(exception.ToString());
                Debug.Log("Failed to initialize office application.");
            }

            if (this.application == null)
            {
                return false;
            }

            if (this.document == null)
            {
                Diagnostics.Debug.Log("Load word document '{0}'.", this.InputFilePath);

                this.document = this.application.Documents.Open(this.InputFilePath, System.Reflection.Missing.Value, true);
            }

            return this.document != null;
        }
    }
}
