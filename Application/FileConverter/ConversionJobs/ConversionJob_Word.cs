// <copyright file="ConversionJob_Word.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using FileConverter.Diagnostics;

    using Markdig;

    using Word = NetOffice.WordApi;

    public class ConversionJob_Word : ConversionJob_Office
    {
        private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        private Word.Document document;
        private Word.Application application;

        private string intermediateFilePath = string.Empty;
        private string markdownHtmlFilePath = null;
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
            if (this.ConversionPreset.OutputType == OutputType.Pdf || this.ConversionPreset.OutputType == OutputType.Docx)
            {
                return 1;
            }

            if (!this.TryLoadDocumentIfNecessary())
            {
                return 1;
            }

            int pagesCount = this.document.ComputeStatistics(Word.Enums.WdStatistic.wdStatisticPages);

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
            else if (this.ConversionPreset.OutputType != OutputType.Docx)
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

            if (this.ConversionPreset.OutputType == OutputType.Docx)
            {
                // Documents opened from html (markdown) only link their pictures; embed them so the docx is self-contained.
                // Indexed access: NetOffice's foreach enumerator needs an initialized type factory.
                int shapesCount = this.document.InlineShapes.Count;
                for (int index = 1; index <= shapesCount; index++)
                {
                    Word.InlineShape shape = this.document.InlineShapes[index];
                    if (shape.LinkFormat != null)
                    {
                        shape.LinkFormat.SavePictureWithDocument = true;
                        shape.LinkFormat.BreakLink();
                    }
                }

                this.document.ActiveWindow.View.Type = Word.Enums.WdViewType.wdPrintView;

                Debug.Log("Save word document as docx.");
                this.document.SaveAs2(this.OutputFilePath, Word.Enums.WdSaveFormat.wdFormatXMLDocument);

                this.CloseDocument();
                return;
            }

            Debug.Log("Convert word document to pdf.");
            // this.document.ExportAsFixedFormat(this.intermediateFilePath, Word.WdExportFormat.wdExportFormatPDF);
            this.document.ExportAsFixedFormat(this.intermediateFilePath,
                Word.Enums.WdExportFormat.wdExportFormatPDF, 
                false, 
                Word.Enums.WdExportOptimizeFor.wdExportOptimizeForPrint, 
                Word.Enums.WdExportRange.wdExportAllDocument, 
                1, 1, 
                Word.Enums.WdExportItem.wdExportDocumentContent, 
                true, 
                true, 
                Word.Enums.WdExportCreateBookmarks.wdExportCreateHeadingBookmarks, 
                true);

            this.CloseDocument();

            if (this.pdf2ImageConversionJob != null)
            {
                if (!System.IO.File.Exists(this.intermediateFilePath))
                {
                    this.ConversionFailed(Properties.Resources.ErrorCantFindOutputFiles);
                    return;
                }

                Task updateProgress = this.UpdateProgress();

                Debug.Log("Convert pdf to images.");

                this.pdf2ImageConversionJob.StartConversion();

                if (this.pdf2ImageConversionJob.State != ConversionState.Done)
                {
                    this.ConversionFailed(this.pdf2ImageConversionJob.ErrorMessage);
                    return;
                }

                if (!string.IsNullOrEmpty(this.intermediateFilePath))
                {
                    Debug.Log($"Delete intermediate file {this.intermediateFilePath}.");

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
            Debug.Log("Instantiate word application via interop.");
            this.application = new Word.Application
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

        protected override void OnConversionFailed()
        {
            base.OnConversionFailed();

            this.DeleteMarkdownHtmlFile();
        }

        private static string MarkdownToHtml(string markdown, string title, string baseDirectory)
        {
            // The html is written in the temp folder: <base> keeps the markdown's relative image paths resolving against its own folder.
            string baseUri = new Uri(baseDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar).AbsoluteUri;

            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\">" +
                   $"<title>{WebUtility.HtmlEncode(title)}</title><base href=\"{WebUtility.HtmlEncode(baseUri)}\">" +
                   "<style>body{font-family:Calibri,sans-serif;font-size:11pt}" +
                   "pre,code{font-family:Consolas,monospace;font-size:10pt;background:#f3f3f3}" +
                   "table{border-collapse:collapse}th,td{border:1px solid #999;padding:4px 8px}" +
                   "blockquote{border-left:3px solid #ccc;margin-left:0;padding-left:12px;color:#555}</style>" +
                   "</head><body>" + Markdown.ToHtml(markdown, MarkdownPipeline) + "</body></html>";
        }

        private void CloseDocument()
        {
            Debug.Log($"Close word document '{this.InputFilePath}'.");
            this.document.Close(Word.Enums.WdSaveOptions.wdDoNotSaveChanges);
            this.document = null;

            this.ReleaseOfficeApplicationInstanceIfNeeded();
            this.DeleteMarkdownHtmlFile();
        }

        private void DeleteMarkdownHtmlFile()
        {
            if (this.markdownHtmlFilePath != null && File.Exists(this.markdownHtmlFilePath))
            {
                Debug.Log($"Delete intermediate file {this.markdownHtmlFilePath}.");
                try
                {
                    File.Delete(this.markdownHtmlFilePath);
                }
                catch (IOException exception)
                {
                    // Word may still hold the file right after a failed conversion; a stray file in the temp folder is harmless.
                    Debug.Log($"Can't delete intermediate file: {exception.Message}");
                }
            }

            this.markdownHtmlFilePath = null;
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
                string documentPath = this.InputFilePath;
                if (Path.GetExtension(this.InputFilePath).ToLowerInvariant() == ".md")
                {
                    // Word can't read markdown: render it to html first, which Word opens natively.
                    this.markdownHtmlFilePath = PathHelpers.GenerateUniquePath(Path.GetTempPath() + Path.GetFileNameWithoutExtension(this.InputFilePath) + ".html");
                    Debug.Log($"Convert markdown '{this.InputFilePath}' to html '{this.markdownHtmlFilePath}'.");

                    string html = MarkdownToHtml(File.ReadAllText(this.InputFilePath), Path.GetFileNameWithoutExtension(this.InputFilePath), Path.GetDirectoryName(Path.GetFullPath(this.InputFilePath)));
                    File.WriteAllText(this.markdownHtmlFilePath, html, new UTF8Encoding(true));
                    documentPath = this.markdownHtmlFilePath;
                }

                Debug.Log($"Load word document '{documentPath}'.");

                this.document = this.application.Documents.Open(documentPath, System.Reflection.Missing.Value, true);
            }

            return this.document != null;
        }
    }
}
