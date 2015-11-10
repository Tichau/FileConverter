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
            base.Initialize();

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            this.ffmpegProcessStartInfo = null;

            string applicationDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string ffmpegPath = string.Format("{0}\\ffmpeg.exe", applicationDirectory);
            if (!System.IO.File.Exists(ffmpegPath))
            {
                this.ConvertionFailed("Can't find ffmpeg executable. You should try to reinstall the application.");
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
                case OutputType.Wav:
                    {
                        EncodingMode encodingMode = this.ConversionPreset.GetSettingsValue<EncodingMode>(ConversionPreset.ConversionSettingKeys.AudioEncodingMode);
                        string encoderArgs = string.Format("-acodec {0}", this.WAVEncodingToCodecArgument(encodingMode));
                        arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);
                    }

                    break;

                case OutputType.Mp3:
                    {
                        string encoderArgs = string.Empty;
                        EncodingMode encodingMode = this.ConversionPreset.GetSettingsValue<EncodingMode>(ConversionPreset.ConversionSettingKeys.AudioEncodingMode);
                        int encodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);
                        switch (encodingMode)
                        {
                            case EncodingMode.Mp3VBR:
                                encoderArgs = string.Format("-codec:a libmp3lame -q:a {0}", this.MP3VBRBitrateToQualityIndex(encodingQuality));
                                break;

                            case EncodingMode.Mp3CBR:
                                encoderArgs = string.Format("-codec:a libmp3lame -b:a {0}k", encodingQuality);
                                break;

                            default:
                                break;
                        }

                        arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);
                    }

                break;

                case OutputType.Ogg:
                    {
                        int encodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);
                        string encoderArgs = string.Format("-codec:a libvorbis -qscale:a {0}", this.OGGVBRBitrateToQualityIndex(encodingQuality));
                        arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);
                    }

                break;

                case OutputType.Flac:
                    {
                        arguments = string.Format("-n -stats -i \"{0}\" \"{1}\"", this.InputFilePath, this.OutputFilePath);
                    }

                    break;

                case OutputType.Mkv:
                    {
                        int videoEncodingQuality = this.ConversionPreset.GetSettingsValue<int>("VideoQuality");
                        string videoEncodingSpeed = this.ConversionPreset.GetSettingsValue<string>("VideoEncodingSpeed");
                        int audioEncodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);
                        string encoderArgs = string.Format("-c:v libx264 -preset {0} -crf {1} -c:a aac -q:a {2} -strict experimental", this.H264EncodingSpeedToPreset(videoEncodingSpeed), this.H264QualityToCRF(videoEncodingQuality), this.AACBitrateToQualityIndex(audioEncodingQuality));
                        arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);
                    }

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

        private int MP3VBRBitrateToQualityIndex(int bitrate)
        {
            switch (bitrate)
            {
                case 245:
                    return 0;

                case 225:
                    return 1;

                case 190:
                    return 2;

                case 175:
                    return 3;

                case 165:
                    return 4;

                case 130:
                    return 5;

                case 115:
                    return 6;

                case 100:
                    return 7;

                case 85:
                    return 8;

                case 65:
                    return 9;
            }

            throw new Exception("Unknown VBR bitrate.");
        }

        /// <summary>
        /// Convert bitrate in <c>vorbis</c> encoder quality index.
        /// </summary>
        /// <param name="bitrate">Wanted bitrate value.</param>
        /// <returns>The <c>vorbis</c> encoder quality index associated to the given bitrate value.</returns>
        /// http://wiki.hydrogenaud.io/index.php?title=Recommended_Ogg_Vorbis
        private int OGGVBRBitrateToQualityIndex(int bitrate)
        {
            switch (bitrate)
            {
                case 500:
                    return 10;

                case 320:
                    return 9;

                case 256:
                    return 8;

                case 224:
                    return 7;

                case 192:
                    return 6;

                case 160:
                    return 5;

                case 128:
                    return 4;

                case 112:
                    return 3;

                case 96:
                    return 2;

                case 80:
                    return 1;

                case 64:
                    return 0;

                case 48:
                    return -1;

                case 32:
                    return -2;
            }

            throw new Exception("Unknown VBR bitrate.");
        }

        /// <summary>
        /// Convert encoding mode setting to <c>ffmpeg</c> argument option.
        /// </summary>
        /// <param name="encoding">The encoding mode setting.</param>
        /// <returns>Returns the <c>ffmpeg</c> argument corresponding to the given encoding mode.</returns>
        /// https://trac.ffmpeg.org/wiki/audio%20types
        private string WAVEncodingToCodecArgument(EncodingMode encoding)
        {
            switch (encoding)
            {
                case EncodingMode.Wav8:
                    return "pcm_s8le";

                case EncodingMode.Wav16:
                    return "pcm_s16le";

                case EncodingMode.Wav24:
                    return "pcm_s24le";

                case EncodingMode.Wav32:
                    return "pcm_s32le";
            }

            throw new Exception("Unknown Wav encoding.");
        }

        /// <summary>
        /// Convert video quality index to H264 constant rate factor.
        /// </summary>
        /// <param name="quality">The quality index.</param>
        /// <returns>Returns the H264 constant rate factor.</returns>
        /// The range of the quantizer scale is 0-51: where 0 is lossless, 23 is default, and 51 is worst possible. 
        /// A lower value is a higher quality and a subjectively sane range is 18-28. 
        /// https://trac.ffmpeg.org/wiki/Encode/H.264
        private int H264QualityToCRF(int quality)
        {
            return 51 - quality;
        }

        /// <summary>
        /// Convert video encoding speed to H264 preset.
        /// </summary>
        /// <param name="encodingSpeed">The encoding speed.</param>
        /// <returns>The H264 preset.</returns>
        private string H264EncodingSpeedToPreset(string encodingSpeed)
        {
            switch (encodingSpeed)
            {
                case "Ultra Fast":
                    return "ultrafast";

                case "Super Fast":
                    return "superfast";

                case "Very Fast":
                    return "veryfast";

                case "Faster":
                    return "faster";

                case "Fast":
                    return "fast";

                case "Medium":
                    return "medium";

                case "Slow":
                    return "slow";

                case "Slower":
                    return "slower";

                case "Very Slow":
                    return "veryslow";
            }

            throw new Exception("Unknown H264 encoding speed.");
        }

        private int AACBitrateToQualityIndex(int bitrate)
        {
            switch (bitrate)
            {
                case 52:
                    return 1;

                case 72:
                    return 2;

                case 104:
                    return 3;

                case 136:
                    return 4;

                case 208:
                    return 5;
            }

            throw new Exception("Unknown VBR bitrate.");
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
