// <copyright file="ConversionJob_FFMPEG.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;

    public class ConversionJob_FFMPEG : ConversionJob
    {
        private readonly Regex durationRegex = new Regex(@"Duration:\s*([0-9][0-9]):([0-9][0-9]):([0-9][0-9])\.([0-9][0-9]),.*bitrate:\s*([0-9]+) kb\/s");
        private readonly Regex progressRegex = new Regex(@"size=\s*([0-9]+)kB\s+time=([0-9][0-9]):([0-9][0-9]):([0-9][0-9]).([0-9][0-9])\s+bitrate=\s*([0-9]+.[0-9])kbits\/s");

        private TimeSpan fileDuration;
        private TimeSpan actualConvertedDuration;

        private ProcessStartInfo ffmpegProcessStartInfo;

        public ConversionJob_FFMPEG() : base()
        {
        }

        public ConversionJob_FFMPEG(ConversionPreset conversionPreset) : base(conversionPreset)
        {
        }

        protected override void Initialize()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

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

            string arguments = string.Empty;
            switch (this.ConversionPreset.OutputType)
            {
                case OutputType.Mp3:
                    arguments = string.Format("-n -stats -i \"{0}\" -qscale:a 2 \"{1}\"", this.InputFilePath, this.OutputFilePath);
                    break;

                case OutputType.Ogg:
                    arguments = string.Format("-n -stats -i \"{0}\" -acodec libvorbis -qscale:a 2 \"{1}\"", this.InputFilePath, this.OutputFilePath);
                    break;

                case OutputType.Flac:
                case OutputType.Wav:
                    arguments = string.Format("-n -stats -i \"{0}\" \"{1}\"", this.InputFilePath, this.OutputFilePath);
                    break;

                default:
                    throw new NotImplementedException("Converter not implemented for output file type " + this.ConversionPreset.OutputType);
            }

            if (string.IsNullOrEmpty(arguments))
            {
                throw new Exception("Invalid ffmpeg process arguments.");
            }

            this.ffmpegProcessStartInfo.Arguments = arguments;
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            Diagnostics.Log("Convert file {0} to {1}.", this.InputFilePath, this.OutputFilePath);
            Diagnostics.Log(string.Empty);
            Diagnostics.Log("Execute command: {0} {1}.", this.ffmpegProcessStartInfo.FileName, this.ffmpegProcessStartInfo.Arguments);

            try
            {
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
                this.ConvertionFailed("Failed to launch FFMPEG process.");
                throw;
            }
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
                this.ConvertionFailed(input);
            }
        }
    }
}
