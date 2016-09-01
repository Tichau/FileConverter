// <copyright file="ConversionJob_ImageMagick.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;

    public class ConversionJob_Word : ConversionJob
    {
        private static Microsoft.Office.Interop.Word.Application wordApplication;
        private static int wordApplicationRefCount = 0;

        public ConversionJob_Word() : base()
        {
        }

        public ConversionJob_Word(ConversionPreset conversionPreset) : base(conversionPreset)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            if (this.ConversionPreset.OutputType != OutputType.Pdf)
            {
                this.ConversionFailed("This conversion is not allowed");
                return;
            }

            System.Threading.Interlocked.Increment(ref ConversionJob_Word.wordApplicationRefCount);
            if (ConversionJob_Word.wordApplication == null)
            {
                Diagnostics.Debug.Log("Instantiate word application via interop.");
                ConversionJob_Word.wordApplication = new Microsoft.Office.Interop.Word.Application
                {
                    Visible = false
                };
            }
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            this.UserState = Properties.Resources.ConversionStateReadDocument;

            object inputFilePath = this.InputFilePath;
            Microsoft.Office.Interop.Word.Document document = ConversionJob_Word.wordApplication.Documents.Open(ref inputFilePath);

            // Make this document the active document.
            document.Activate();

            this.UserState = Properties.Resources.ConversionStateConversion;

            object outputFilePath = this.OutputFilePath;
            object outputFileFormat = Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatPDF;
            document.SaveAs(ref outputFilePath, ref outputFileFormat);

            System.Threading.Interlocked.Decrement(ref ConversionJob_Word.wordApplicationRefCount);
            if (ConversionJob_Word.wordApplicationRefCount == 0)
            {
                Diagnostics.Debug.Log("Quit word application via interop.");
                ConversionJob_Word.wordApplication.Quit();
            }
        }
    }
}
