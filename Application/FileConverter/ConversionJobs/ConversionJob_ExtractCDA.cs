// <copyright file="ConversionJob_ExtractCDA.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;
    using System.Threading;

    using Ripper;

    using WaveLib;
    using Yeti.MMedia;

    public class ConversionJob_ExtractCDA : ConversionJob
    {
        private Ripper.CDDrive cdDrive;
        private int cdaTrackNumber = -1;
        private WaveWriter waveWriter;
        private string intermediateFilePath;
        private ConversionJob compressionConversionJob;
        private System.Threading.Thread compressionThread;

        public ConversionJob_ExtractCDA() : base()
        {
        }

        public ConversionJob_ExtractCDA(ConversionPreset conversionPreset) : base(conversionPreset)
        {
        }

        protected override InputPostConversionAction InputPostConversionAction
        {
            get
            {
                return InputPostConversionAction.None;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            // Retrieve and check drive letter.
            string pathDriveLetter = PathHelpers.GetPathDriveLetter(this.InputFilePath);
            if (pathDriveLetter.Length == 0)
            {
                this.ConversionFailed("Can't retrieve input path drive letter.");
                return;
            }

            char driveLetter = pathDriveLetter[0];

            this.cdDrive = new Ripper.CDDrive();
            this.cdDrive.CDRemoved += new EventHandler(this.CdDriveCdRemoved);

            bool driveLetterFound = false;
            char[] driveLetters = Ripper.CDDrive.GetCDDriveLetters();
            for (int index = 0; index < driveLetters.Length; index++)
            {
                driveLetterFound |= driveLetters[index] == driveLetter;
            }

            if (!driveLetterFound)
            {
                this.ConversionFailed(string.Format("Invalid drive letter {0}.", driveLetter));
                return;
            }

            // Retrieve and track number.
            try
            {
                this.cdaTrackNumber = PathHelpers.GetCDATrackNumber(this.InputFilePath);
            }
            catch (Exception)
            {
                this.ConversionFailed(string.Format("Can't retrieve the track number from input path '{0}'.", this.InputFilePath));
                return;
            }

            if (this.cdDrive.IsOpened)
            {
                this.ConversionFailed(string.Format("CD drive already used."));
                return;
            }

            if (!this.cdDrive.Open(driveLetter))
            {
                this.ConversionFailed(string.Format("Fail to open cd drive {0}.", driveLetter));
                return;
            }

            // Generate intermediate file path.
            string fileName = Path.GetFileName(this.OutputFilePath);
            string tempPath = Path.GetTempPath();
            this.intermediateFilePath = PathHelpers.GenerateUniquePath(tempPath + fileName + ".wav");

            // Sub conversion job (for compression).
            this.compressionConversionJob = ConversionJobFactory.Create(this.ConversionPreset, this.intermediateFilePath);
            this.compressionConversionJob.PrepareConversion(this.intermediateFilePath, this.OutputFilePath);
            this.compressionThread = new Thread(this.CompressAsync);
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            Diagnostics.Log("Starting CDA extraction.");

            this.UserState = "Extraction";

            if (!this.cdDrive.IsCDReady())
            {
                this.ConversionFailed(string.Format("CD drive is not ready."));
                return;
            }

            if (!this.cdDrive.Refresh())
            {
                this.ConversionFailed(string.Format("Can't refresh CD drive data."));
                return;
            }

            if (!this.cdDrive.LockCD())
            {
                this.ConversionFailed(string.Format("Can't lock cd."));
                return;
            }

            WaveFormat waveFormat = new WaveFormat(44100, 16, 2);

            using (Stream waveStream = new FileStream(this.intermediateFilePath, FileMode.Create, FileAccess.Write))
            using (this.waveWriter = new WaveWriter(waveStream, waveFormat, this.cdDrive.TrackSize(this.cdaTrackNumber)))
            {
                this.cdDrive.ReadTrack(this.cdaTrackNumber, this.WriteWaveData, this.CdReadProgress);
            }

            this.waveWriter = null;

            this.cdDrive.UnLockCD();

            this.cdDrive.Close();

            if (!File.Exists(this.intermediateFilePath))
            {
                this.ConversionFailed("Extraction failed.");
                return;
            }

            Diagnostics.Log("CDA extracted to {0}.", this.intermediateFilePath);
            Diagnostics.Log(string.Empty);
            Diagnostics.Log("Start compression.");

            this.UserState = "Conversion";

            this.compressionThread.Start();

            while (this.compressionConversionJob.State != ConversionState.Done &&
                this.compressionConversionJob.State != ConversionState.Failed)
            {
                this.Progress = this.compressionConversionJob.Progress;
            }

            if (this.compressionConversionJob.State == ConversionState.Failed)
            {
                this.ConversionFailed(this.compressionConversionJob.ErrorMessage);
                return;
            }

            Diagnostics.Log(string.Empty);
            Diagnostics.Log("Delete intermediate file {0}.", this.intermediateFilePath);

            File.Delete(this.intermediateFilePath);
        }

        private void WriteWaveData(object sender, DataReadEventArgs eventArgs)
        {
            this.waveWriter?.Write(eventArgs.Data, 0, (int)eventArgs.DataSize);
        }

        private void CdReadProgress(object sender, ReadProgressEventArgs eventArgs)
        {
            this.Progress = (float)eventArgs.BytesRead / (float)eventArgs.Bytes2Read;

            eventArgs.CancelRead |= this.State != ConversionState.InProgress;
        }

        private void CdDriveCdRemoved(object sender, System.EventArgs eventArgs)
        {
            this.ConversionFailed("The CD has been ejected.");
        }

        private void CompressAsync()
        {
            this.compressionConversionJob.StartConvertion();
        }
    }
}
