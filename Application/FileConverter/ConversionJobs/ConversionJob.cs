// <copyright file="ConversionJob.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ConversionJob : INotifyPropertyChanged
    {
        private float progress = 0f;
        private ConversionState state = ConversionState.Unknown;
        private string errorMessage = string.Empty;

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

        public void PrepareConversion(string inputFilePath)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentNullException("inputFilePath");
            }

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            this.InputFilePath = inputFilePath;
            this.OutputFilePath = this.ConversionPreset.GenerateOutputFilePath(inputFilePath);
            string presetError = this.ConversionPreset.Error;
            if (!string.IsNullOrEmpty(presetError))
            {
                this.State = ConversionState.Failed;
                this.ErrorMessage = presetError;
                return;
            }

            this.Initialize();

            this.State = ConversionState.Ready;
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

            Diagnostics.Log("Convert file {0} to {1}.", this.InputFilePath, this.OutputFilePath);

            this.State = ConversionState.InProgress;

            this.Convert();

            if (this.State != ConversionState.Failed)
            {
                // Convertion succeed !
                this.Progress = 1f;
                this.State = ConversionState.Done;
                Diagnostics.Log("\nDone!");
            }
        }

        protected virtual void Convert()
        {
        }

        protected virtual void Initialize()
        {
        }

        protected void ConvertionFailed(string exitingMessage)
        {
            this.State = ConversionState.Failed;
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
