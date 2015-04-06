// <copyright file="ConversionJob.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    public class ConversionJob : INotifyPropertyChanged
    {
        private readonly Regex durationRegex = new Regex(@"Duration:\s*([0-9][0-9]):([0-9][0-9]):([0-9][0-9])\.([0-9][0-9]),.*bitrate:\s*([0-9]+) kb\/s");
        private readonly Regex progressRegex = new Regex(@"size=\s*([0-9]+)kB\s+time=([0-9][0-9]):([0-9][0-9]):([0-9][0-9]).([0-9][0-9])\s+bitrate=\s*([0-9]+.[0-9])kbits\/s");

        private TimeSpan fileDuration;
        private TimeSpan actualConvertedDuration;

        private ProcessStartInfo ffmpegProcessStartInfo;

        private float progress;

        private ConversionState state;

        private string exitingMessage;

        public ConversionJob()
        {
            this.OutputType = OutputType.None;
            this.progress = 0f;
            this.InputFilePath = string.Empty;
            this.OutputFilePath = string.Empty;
            this.State = ConversionState.Unknown;
            this.ExitingMessage = string.Empty;
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

        public OutputType OutputType
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

            private set
            {
                this.progress = value;
                this.NotifyPropertyChanged();
            }
        }

        public string ExitingMessage
        {
            get
            {
                return this.exitingMessage;
            }

            set
            {
                this.exitingMessage = value;
                this.NotifyPropertyChanged();
            }
        }

        public void Initialize(OutputType outputType, string inputFileName, string outputFileName)
        {
            if (outputType == OutputType.None)
            {
                throw new ArgumentException("The output type must be valid.", "outputType");
            }

            this.OutputType = outputType;
            this.ffmpegProcessStartInfo = null;

            string applicationDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string ffmpegPath = string.Format("{0}\\ffmpeg.exe", applicationDirectory);
            if (!System.IO.File.Exists(ffmpegPath))
            {
                Diagnostics.Log("Can't find ffmpeg executable ({0}). Try to reinstall the application.", ffmpegPath);
                return;
            }

            this.ffmpegProcessStartInfo = new ProcessStartInfo(ffmpegPath);

            this.ffmpegProcessStartInfo.CreateNoWindow = true;
            this.ffmpegProcessStartInfo.UseShellExecute = false;
            this.ffmpegProcessStartInfo.RedirectStandardOutput = true;
            this.ffmpegProcessStartInfo.RedirectStandardError = true;

            this.InputFilePath = inputFileName;
            this.OutputFilePath = outputFileName;

            this.State = ConversionState.Ready;
        }

        public void StartConvertion()
        {
            if (this.OutputType == OutputType.None)
            {
                throw new Exception("The converter is not initialized.");
            }

            Diagnostics.Log("Convert file {0} to {1}.", this.InputFilePath, this.OutputFilePath);

            string arguments = string.Empty;
            switch (this.OutputType)
            {
                case OutputType.Mp3:
                    arguments = string.Format("-n -stats -i \"{0}\" -qscale:a 5 \"{1}\"", this.InputFilePath, this.OutputFilePath);
                    break;

                case OutputType.Ogg:
                    arguments = string.Format("-n -stats -i \"{0}\" -acodec libvorbis -qscale:a 5 \"{1}\"", this.InputFilePath, this.OutputFilePath);
                    break;

                case OutputType.Flac:
                    arguments = string.Format("-n -stats -i \"{0}\" \"{1}\"", this.InputFilePath, this.OutputFilePath);
                    break;
                        
                case OutputType.Wav:
                    arguments = string.Format("-n -stats -i \"{0}\" \"{1}\"", this.InputFilePath, this.OutputFilePath);
                    break;

                default:
                    throw new NotImplementedException("Converter not implemented for output file type " + this.OutputType);
            }

            if (string.IsNullOrEmpty(arguments) || this.ffmpegProcessStartInfo == null)
            {
                return;
            }

            this.State = ConversionState.InProgress;

            try
            {
                Diagnostics.Log(string.Empty);
                this.ffmpegProcessStartInfo.Arguments = arguments;
                Diagnostics.Log("Execute command: {0} {1}.", this.ffmpegProcessStartInfo.FileName, this.ffmpegProcessStartInfo.Arguments);
                using (Process exeProcess = Process.Start(this.ffmpegProcessStartInfo))
                {
                    using (StreamReader reader = exeProcess.StandardError)
                    {
                        while (!reader.EndOfStream)
                        {
                            string result = reader.ReadLine();

                            this.ParseFFMPEGOutput(result);

                            Diagnostics.Log("ffmpeg output: {0}", result);                            
                        }
                    }

                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                this.State = ConversionState.Failed;
                throw;
            }

            if (!string.IsNullOrEmpty(this.ExitingMessage))
            {
                this.State = ConversionState.Failed;
                return;
            }

            this.Progress = 1f;
            this.State = ConversionState.Done;

            Diagnostics.Log("\nDone!");
        }

        private void ParseFFMPEGOutput(string input)
        {
            Match match = this.durationRegex.Match(input);
            if (match.Success && match.Groups.Count >= 6)
            {
                int hours = int.Parse(match.Groups[1].Value);
                int minutes = int.Parse(match.Groups[2].Value);
                int seconds = int.Parse(match.Groups[3].Value);
                int milliseconds = int.Parse(match.Groups[4].Value);
                float bitrate = float.Parse(match.Groups[5].Value);
                this.fileDuration = new TimeSpan(0, hours, minutes, seconds, milliseconds);
                return;
            }

            match = this.progressRegex.Match(input);
            if (match.Success && match.Groups.Count >= 7)
            {
                int size = int.Parse(match.Groups[1].Value);
                int hours = int.Parse(match.Groups[2].Value);
                int minutes = int.Parse(match.Groups[3].Value);
                int seconds = int.Parse(match.Groups[4].Value);
                int milliseconds = int.Parse(match.Groups[5].Value);
                float bitrate = 0f;
                float.TryParse(match.Groups[6].Value, out bitrate);

                this.actualConvertedDuration = new TimeSpan(0, hours, minutes, seconds, milliseconds);

                this.Progress = this.actualConvertedDuration.Ticks / (float)this.fileDuration.Ticks;
                return;
            }

            if (input.Contains("Exiting."))
            {
                this.ExitingMessage = input;
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
