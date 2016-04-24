// <copyright file="ConversionJob.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Text;

    using FileConverter.Diagnostics;

    public class ConversionJob : INotifyPropertyChanged
    {
        private float progress = 0f;
        private ConversionState state = ConversionState.Unknown;
        private string errorMessage = string.Empty;
        private string initialInputPath = string.Empty;
        private string userState = string.Empty;
        private CancelConversionJobCommand cancelCommand;

        public ConversionJob()
        {
            this.State = ConversionState.Unknown;
            this.ConversionPreset = null;
            this.InputFilePath = string.Empty;
        }

        public ConversionJob(ConversionPreset conversionPreset) : this()
        {
            if (conversionPreset == null)
            {
                throw new ArgumentNullException("conversionPreset");
            }

            this.ConversionPreset = conversionPreset;
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
            get;
            private set;
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

        public virtual bool CanStartConversion(ConversionFlags conversionFlags)
        {
            return (conversionFlags & ConversionFlags.CdDriveExtraction) == 0;
        }

        public void PrepareConversion(string inputFilePath, string outputFilePath = null)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentNullException("inputFilePath");
            }

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            string extension = System.IO.Path.GetExtension(inputFilePath);
            extension = extension.Substring(1, extension.Length - 1);
            string extensionCategory = PathHelpers.GetExtensionCategory(extension);
            if (!PathHelpers.IsOutputTypeCompatibleWithCategory(this.ConversionPreset.OutputType, extensionCategory))
            {
                this.ConversionFailed("The input file type is not compatible with the output file type.");
                return;
            }

            this.initialInputPath = inputFilePath;
            this.InputFilePath = inputFilePath;
            this.OutputFilePath = outputFilePath ?? this.ConversionPreset.GenerateOutputFilePath(inputFilePath);
            
            if (!PathHelpers.IsPathValid(this.OutputFilePath))
            {
                this.ConversionFailed("Invalid output path generated by output file path template.");
                Debug.Log(string.Format("Invalid output path generated: {0} from input: {1}.", this.OutputFilePath, this.InputFilePath));
                return;
            }

            if (this.OutputFilePath == this.InputFilePath)
            {
                // If the input post conversion action is to move or delete the input file, change its name in order to keep the output name intact.
                if (this.ConversionPreset.InputPostConversionAction == InputPostConversionAction.MoveInArchiveFolder ||
                    this.ConversionPreset.InputPostConversionAction == InputPostConversionAction.Delete)
                {
                    string inputExtension = System.IO.Path.GetExtension(this.InputFilePath);
                    string pathWithoutExtension = this.InputFilePath.Substring(0, this.InputFilePath.Length - inputExtension.Length);
                    this.InputFilePath = PathHelpers.GenerateUniquePath(pathWithoutExtension + "_TEMP" + inputExtension);
                    System.IO.File.Move(inputFilePath, this.InputFilePath);
                }
            }

            // Create output folder that doesn't exist.
            {
                StringBuilder path = new StringBuilder(this.OutputFilePath.Length);
                foreach (string directory in PathHelpers.GetDirectories(this.OutputFilePath))
                {
                    path.Append(directory);
                    path.Append('\\');

                    if (!System.IO.Directory.Exists(path.ToString()))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(path.ToString());
                        }
                        catch (Exception)
                        {
                            this.ConversionFailed("Invalid output path generated by output file path template.");
                            Debug.Log(string.Format("Can't create directories for path {0}", this.OutputFilePath));
                            return;
                        }
                    }
                }
            }

            // Make the output path valid.
            try
            {
                this.OutputFilePath = PathHelpers.GenerateUniquePath(this.OutputFilePath);
            }
            catch (Exception exception)
            {
                this.ConversionFailed("Can't generate a valid output filename.");
                Debug.Log(exception.Message);
                return;
            }

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

            this.UserState = "In Queue";
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
        }

        public virtual void Cancel()
        {
            if (!this.IsCancelable || this.State != ConversionState.InProgress)
            {
                return;
            }

            this.CancelIsRequested = true;
            this.ConversionFailed("Canceled.");
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

            try
            {
                if (System.IO.File.Exists(this.OutputFilePath))
                {
                    System.IO.File.Delete(this.OutputFilePath);
                }
            }
            catch (Exception exception)
            {
                Debug.Log("Can't delete file '{0}' after conversion job failure.", this.OutputFilePath);
                Debug.Log("An exception as been thrown: {0}.", exception.ToString());
            }
        }

        protected virtual void OnConversionSucceed()
        {
            Debug.Log("Conversion Succeed!");

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
            this.UserState = "Done";
            Debug.Log("Conversion Done!");
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
            this.UserState = "Failed";
            this.ErrorMessage = exitingMessage;
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
