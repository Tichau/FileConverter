// <copyright file="ConversionJob.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Text;

    using FileConverter.Commands;
    using FileConverter.Diagnostics;

    public class ConversionJob : INotifyPropertyChanged
    {
        private float progress = 0f;
        private ConversionState state = ConversionState.Unknown;
        private string errorMessage = string.Empty;
        private string userState = string.Empty;
        private CancelConversionJobCommand cancelCommand;

        private string initialInputPath = string.Empty;
        private int currentOuputFilePathIndex;

        public ConversionJob()
        {
            this.State = ConversionState.Unknown;
            this.ConversionPreset = null;
            this.initialInputPath = string.Empty;
            this.InputFilePath = string.Empty;
        }

        public ConversionJob(ConversionPreset conversionPreset, string inputFilePath) : this()
        {
            if (conversionPreset == null)
            {
                throw new ArgumentNullException(nameof(conversionPreset));
            }

            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentNullException(nameof(inputFilePath));
            }

            this.initialInputPath = inputFilePath;
            this.InputFilePath = inputFilePath;
            this.ConversionPreset = conversionPreset;
            this.UserState = Properties.Resources.ConversionStatePrepareConversion;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public enum ConversionState
        {
            Unknown,

            Ready,
            InProgress,
            Done,
            Failed,
        }

        public ConversionPreset ConversionPreset
        {
            get;
            private set;
        }

        public string InputFilePath
        {
            get;
            private set;
        }

        public string OutputFilePath
        {
            get
            {
                if (this.OutputFilePaths == null || this.OutputFilePaths.Length == 0)
                {
                    return string.Empty;
                }

                if (this.CurrentOuputFilePathIndex < 0)
                {
                    return this.OutputFilePaths[0];
                }

                if (this.CurrentOuputFilePathIndex >= this.OutputFilePaths.Length)
                {
                    return this.OutputFilePaths[this.OutputFilePaths.Length - 1];
                }

                return this.OutputFilePaths[this.CurrentOuputFilePathIndex];
            }
        }

        public ConversionState State
        {
            get
            {
                return this.state;
            }

            private set
            {
                this.state = value;
                this.NotifyPropertyChanged();
            }
        }

        public string UserState
        {
            get
            {
                return this.userState;
            }

            protected set
            {
                this.userState = value;
                this.NotifyPropertyChanged();
            }
        }

        public float Progress
        {
            get
            {
                return this.progress;
            }

            protected set
            {
                this.progress = value;
                this.NotifyPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get
            {
                return this.errorMessage;
            }

            private set
            {
                this.errorMessage = value;
                this.NotifyPropertyChanged();
            }
        }

        public ConversionFlags StateFlags
        {
            get;
            protected set;
        }

        public CancelConversionJobCommand CancelCommand
        {
            get
            {
                if (this.cancelCommand == null)
                {
                    this.cancelCommand = new CancelConversionJobCommand(this);
                }

                return this.cancelCommand;
            }
        }

        public bool IsCancelable
        {
            get;
            protected set;
        }

        protected bool CancelIsRequested
        {
            get;
            private set;
        }

        protected int CurrentOuputFilePathIndex
        {
            get
            {
                return this.currentOuputFilePathIndex;
            }

            set
            {
                this.currentOuputFilePathIndex = value;
                this.NotifyPropertyChanged(nameof(this.OutputFilePath));
            }
        }

        protected virtual InputPostConversionAction InputPostConversionAction
        {
            get
            {
                if (this.ConversionPreset == null)
                {
                    return InputPostConversionAction.None;
                }

                return this.ConversionPreset.InputPostConversionAction;
            }
        }

        protected string[] OutputFilePaths
        {
            get;
            private set;
        }

        public virtual bool CanStartConversion(ConversionFlags conversionFlags)
        {
            return (conversionFlags & ConversionFlags.CdDriveExtraction) == 0;
        }

        public void PrepareConversion(params string[] outputFilePaths)
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            this.InputFilePath = this.initialInputPath;

            string extension = System.IO.Path.GetExtension(this.initialInputPath);
            extension = extension.Substring(1, extension.Length - 1);
            string extensionCategory = Helpers.GetExtensionCategory(extension);
            if (!Helpers.IsOutputTypeCompatibleWithCategory(this.ConversionPreset.OutputType, extensionCategory))
            {
                this.ConversionFailed(Properties.Resources.ErrorInputTypeIncompatibleWithOutputType);
                return;
            }

            this.OutputFilePaths = outputFilePaths;
            if (this.OutputFilePaths.Length == 0)
            {
                int outputFilesCount = this.GetOuputFilesCount();
                this.OutputFilePaths = new string[outputFilesCount];
            }

            for (int index = 0; index < this.OutputFilePaths.Length; index++)
            {
                if (!string.IsNullOrEmpty(this.OutputFilePaths[index]))
                {
                    // Don't generate a path if it has already been set.
                    continue;
                }

                string path = this.ConversionPreset.GenerateOutputFilePath(this.initialInputPath, index + 1, this.OutputFilePaths.Length);

                if (!PathHelpers.IsPathValid(path))
                {
                    this.ConversionFailed(Properties.Resources.ErrorInvalidOutputPath);
                    Debug.Log($"Invalid output path generated: {path} from input: {this.InputFilePath}.");
                    return;
                }

                if (path == this.InputFilePath)
                {
                    // If the input post conversion action is to move or delete the input file, change its name in order to keep the output name intact.
                    if (this.ConversionPreset.InputPostConversionAction == InputPostConversionAction.MoveInArchiveFolder ||
                        this.ConversionPreset.InputPostConversionAction == InputPostConversionAction.Delete)
                    {
                        string inputExtension = System.IO.Path.GetExtension(this.InputFilePath);
                        string pathWithoutExtension = this.InputFilePath.Substring(0, this.InputFilePath.Length - inputExtension.Length);
                        this.InputFilePath = PathHelpers.GenerateUniquePath(pathWithoutExtension + "_TEMP" + inputExtension);
                        System.IO.File.Move(this.initialInputPath, this.InputFilePath);
                    }
                }

                // Create output folders that doesn't exist.
                if (!PathHelpers.CreateFolders(path))
                {
                    this.ConversionFailed(Properties.Resources.ErrorFailToCreateOutputPathFolders);
                    return;
                }

                // Make the output path valid.
                try
                {
                    path = PathHelpers.GenerateUniquePath(path, this.OutputFilePaths);
                }
                catch (Exception exception)
                {
                    this.ConversionFailed(Properties.Resources.ErrorFailToGenerateUniqueOutputPath);
                    Debug.Log(exception.Message);
                    return;
                }

                this.OutputFilePaths[index] = path;
            }

            this.CurrentOuputFilePathIndex = 0;

            // Check if the input file is located on a cd drive.
            if (PathHelpers.IsOnCDDrive(this.InputFilePath))
            {
                this.StateFlags = ConversionFlags.CdDriveExtraction;
            }

            this.Initialize();

            if (this.State == ConversionState.Unknown)
            {
                this.State = ConversionState.Ready;
            }

            Debug.Log("Job initialized: Preset: '{0}' Input: {1} Output: {2}", this.ConversionPreset.Name, this.InputFilePath, this.OutputFilePath);

            if (this.State != ConversionState.Failed)
            {
                this.UserState = Properties.Resources.ConversionStateInQueue;
            }
        }

        public void StartConvertion()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            if (this.State != ConversionState.Ready)
            {
                throw new Exception("Invalid conversion state.");
            }

            Debug.Log("Convert file {0} to {1}.", this.InputFilePath, this.OutputFilePath);

            this.State = ConversionState.InProgress;
            
            try
            {
                this.Convert();
            }
            catch (Exception exception)
            {
                this.ConversionFailed(exception.Message);
            }

            this.StateFlags = ConversionFlags.None;

            if (this.State == ConversionState.Failed)
            {
                this.OnConversionFailed();
            }
            else
            {
                this.OnConversionSucceed();
            }

            if (this.State == ConversionJob.ConversionState.Done && !this.AllOuputFilesExists())
            {
                Debug.LogError(Properties.Resources.ErrorCantFindOutputFiles);
            }
            else if (this.State == ConversionJob.ConversionState.Failed && this.AtLeastOneOuputFilesExists())
            {
                Debug.Log(Properties.Resources.ErrorConversionFailedWithOutput);
            }
        }

        public virtual void Cancel()
        {
            if (!this.IsCancelable || this.State != ConversionState.InProgress)
            {
                return;
            }

            this.CancelIsRequested = true;
            this.ConversionFailed(Properties.Resources.ErrorCanceled);
        }

        protected virtual int GetOuputFilesCount()
        {
            return 1;
        }

        protected virtual void Convert()
        {
        }

        protected virtual void Initialize()
        {
        }

        protected virtual void OnConversionFailed()
        {
            Debug.Log("Conversion Failed.");

            for (int index = 0; index < this.OutputFilePaths.Length; index++)
            {
                string outputFilePath = this.OutputFilePaths[index];
                try
                {
                    if (System.IO.File.Exists(outputFilePath))
                    {
                        System.IO.File.Delete(outputFilePath);
                    }
                }
                catch (Exception exception)
                {
                    Debug.Log("Can't delete file '{0}' after conversion job failure.", outputFilePath);
                    Debug.Log("An exception as been thrown: {0}.", exception.ToString());
                }
            }
        }

        protected virtual void OnConversionSucceed()
        {
            Debug.Log("Conversion Succeed!");

            ChangeOutputFileTimestampToMatchOriginal();

            // Apply the input post conversion action.
            switch (this.InputPostConversionAction)
            {
                case InputPostConversionAction.None:
                    break;

                case InputPostConversionAction.MoveInArchiveFolder:
                    string basePath = System.IO.Path.GetDirectoryName(this.initialInputPath);
                    string inputFilename = System.IO.Path.GetFileName(this.initialInputPath);
                    string archivePath = basePath + "\\" + this.ConversionPreset.ConversionArchiveFolderName;
                    if (!System.IO.Directory.Exists(archivePath))
                    {
                        System.IO.Directory.CreateDirectory(archivePath);
                    }

                    string newPath = PathHelpers.GenerateUniquePath(archivePath + "\\" + inputFilename);
                    System.IO.File.Move(this.InputFilePath, newPath);
                    Debug.Log("Input file moved in archive folder: '{0}'", newPath);
                    break;

                case InputPostConversionAction.Delete:
                    System.IO.File.Delete(this.InputFilePath);
                    Debug.Log("Input file deleted: '{0}'", this.initialInputPath);
                    break;
            }

            Debug.Log(string.Empty);

            this.Progress = 1f;
            this.State = ConversionState.Done;
            this.UserState = Properties.Resources.ConversionStateDone;
            Debug.Log("Conversion Done!");
        }

        private void ChangeOutputFileTimestampToMatchOriginal()
        {
            Debug.Log("Changing output files timestamp to match original timestamp ...");

            var originalFileCreationTime = System.IO.File.GetCreationTimeUtc(this.InputFilePath);
            var originalFileLastAccesTime = System.IO.File.GetLastAccessTimeUtc(this.InputFilePath);
            var originalFileLastWriteTime = System.IO.File.GetLastWriteTimeUtc(this.InputFilePath);
            Debug.Log("  original timestamp: {0}, {1}, {2}", originalFileCreationTime, originalFileLastAccesTime, originalFileLastWriteTime);

            for (int index = 0; index < this.OutputFilePaths.Length; index++)
            {
                string outputFilePath = this.OutputFilePaths[index];
                try
                {
                    System.IO.File.SetCreationTimeUtc(outputFilePath, originalFileCreationTime);
                    System.IO.File.SetLastAccessTimeUtc(outputFilePath, originalFileLastAccesTime);
                    System.IO.File.SetLastWriteTimeUtc(outputFilePath, originalFileLastWriteTime);
                    Debug.Log("  output file '{0}' timestamp changed", outputFilePath);
                }
                catch (Exception exception)
                {
                    Debug.Log("Can't change timestamp from file '{0}'", outputFilePath);
                    Debug.Log("An exception as been thrown: {0}.", exception.ToString());
                }
            }

            Debug.Log("... timestamp matching finished.");
        }

        protected void ConversionFailed(string exitingMessage)
        {
            Debug.Log("Fail: {0}", exitingMessage);

            if (this.State == ConversionState.Failed)
            {
                // Already failed, don't override informations.
                return;    
            }

            this.State = ConversionState.Failed;
            this.UserState = Properties.Resources.ConversionStateFailed;
            this.ErrorMessage = exitingMessage;
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool AllOuputFilesExists()
        {
            for (int index = 0; index < this.OutputFilePaths.Length; index++)
            {
                string outputFilePath = this.OutputFilePaths[index];
                if (!System.IO.File.Exists(outputFilePath))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AtLeastOneOuputFilesExists()
        {
            for (int index = 0; index < this.OutputFilePaths.Length; index++)
            {
                string outputFilePath = this.OutputFilePaths[index];
                if (System.IO.File.Exists(outputFilePath))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
