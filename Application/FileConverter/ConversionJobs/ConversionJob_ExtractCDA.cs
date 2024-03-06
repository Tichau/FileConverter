// <copyright file="ConversionJob_ExtractCDA.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;
    using System.Threading;

    using Diagnostics;
    using Ripper;
    using WaveLib;
    using Yeti.MMedia;

    public class ConversionJob_ExtractCDA : ConversionJob
    {
        private Ripper.CDDrive diskDrive;
        private int cdaTrackNumber = -1;
        private WaveWriter waveWriter;
        private string intermediateFilePath;
        private ConversionJob compressionConversionJob;
        private System.Threading.Thread compressionThread;

        public ConversionJob_ExtractCDA() : base()
        {
        }

        public ConversionJob_ExtractCDA(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        protected override InputPostConversionAction InputPostConversionAction
        {
            get
            {
                return InputPostConversionAction.None;
            }
        }

        public override void Cancel()
        {
            base.Cancel();

            this.compressionConversionJob.Cancel();
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
                this.ConversionFailed(Properties.Resources.ErrorFailToRetrieveInputPathDriveLetter);
                return;
            }

            char driveLetter = pathDriveLetter[0];

            this.diskDrive = new Ripper.CDDrive();
            this.diskDrive.CDRemoved += new EventHandler(this.CdDriveCdRemoved);

            bool driveLetterFound = false;
            char[] driveLetters = Ripper.CDDrive.GetCDDriveLetters();
            for (int index = 0; index < driveLetters.Length; index++)
            {
                driveLetterFound |= driveLetters[index] == driveLetter;
            }

            if (!driveLetterFound)
            {
                Debug.Log($"Invalid drive letter {driveLetter}.");
                this.ConversionFailed(Properties.Resources.ErrorFailToRetrieveInputPathDriveLetter);
                return;
            }

            // Retrieve and track number.
            try
            {
                this.cdaTrackNumber = PathHelpers.GetCDATrackNumber(this.InputFilePath);
            }
            catch (Exception)
            {
                Debug.Log($"Input path: '{this.InputFilePath}'.");
                this.ConversionFailed(Properties.Resources.ErrorFailToRetrieveTrackNumber);
                return;
            }

            if (this.diskDrive.IsOpened)
            {
                this.ConversionFailed(Properties.Resources.ErrorFailToUseCDDriveOpen);
                return;
            }

            if (!this.diskDrive.Open(driveLetter))
            {
                this.ConversionFailed(string.Format(Properties.Resources.ErrorFailToReadCDDrive, driveLetter));
                return;
            }

            // Generate intermediate file path.
            string fileName = Path.GetFileName(this.OutputFilePath);
            string tempPath = Path.GetTempPath();
            this.intermediateFilePath = PathHelpers.GenerateUniquePath(tempPath + fileName + ".wav");

            // Sub conversion job (for compression).
            this.compressionConversionJob = ConversionJobFactory.Create(this.ConversionPreset, this.intermediateFilePath);
            this.compressionConversionJob.PrepareConversion(this.OutputFilePath);
            this.compressionThread = Helpers.InstantiateThread("CDACompressionThread", this.CompressAsync);
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            Debug.Log("Starting CDA extraction.");

            this.UserState = Properties.Resources.ConversionStateExtraction;

            if (!this.diskDrive.IsCDReady())
            {
                this.ConversionFailed(Properties.Resources.ErrorCDDriveNotReady);
                return;
            }

            if (!this.diskDrive.Refresh())
            {
                Debug.Log("Can't refresh CD drive data.");
                this.ConversionFailed(Properties.Resources.ErrorCDDriveNotReady);
                return;
            }

            if (!this.diskDrive.LockCD())
            {
                Debug.Log("Can\'t lock cd.");
                this.ConversionFailed(Properties.Resources.ErrorCDDriveNotReady);
                return;
            }

            WaveFormat waveFormat = new WaveFormat(44100, 16, 2);

            using (Stream waveStream = new FileStream(this.intermediateFilePath, FileMode.Create, FileAccess.Write))
            using (this.waveWriter = new WaveWriter(waveStream, waveFormat, this.diskDrive.TrackSize(this.cdaTrackNumber)))
            {
                this.diskDrive.ReadTrack(this.cdaTrackNumber, this.WriteWaveData, this.CdReadProgress);
            }

            this.waveWriter = null;

            this.diskDrive.UnLockCD();

            this.diskDrive.Close();

            this.StateFlags = ConversionFlags.None;

            if (!File.Exists(this.intermediateFilePath))
            {
                this.ConversionFailed(Properties.Resources.ErrorCDAExtractionFailed);
                return;
            }

            Debug.Log("CDA extracted to {0}.", this.intermediateFilePath);
            Debug.Log(string.Empty);
            Debug.Log("Start compression.");

            this.UserState = Properties.Resources.ConversionStateConversion;

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

            Debug.Log(string.Empty);
            Debug.Log("Delete intermediate file {0}.", this.intermediateFilePath);

            File.Delete(this.intermediateFilePath);
        }

        private void WriteWaveData(object sender, DataReadEventArgs eventArgs)
        {
            this.waveWriter?.Write(eventArgs.Data, 0, (int)eventArgs.DataSize);
        }

        private void CdReadProgress(object sender, ReadProgressEventArgs eventArgs)
        {
            if (this.CancelIsRequested)
            {
                eventArgs.CancelRead = true;
                return;
            }

            this.Progress = (float)eventArgs.BytesRead / (float)eventArgs.Bytes2Read;

            eventArgs.CancelRead |= this.State != ConversionState.InProgress;
        }

        private void CdDriveCdRemoved(object sender, System.EventArgs eventArgs)
        {
            this.ConversionFailed("The CD has been ejected.");
        }

        private void CompressAsync()
        {
            this.compressionConversionJob.StartConversion();
        }
    }
}
