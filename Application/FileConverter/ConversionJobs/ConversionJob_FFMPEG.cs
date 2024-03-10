// <copyright file="ConversionJob_FFMPEG.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    using FileConverter.Controls;

    public partial class ConversionJob_FFMPEG : ConversionJob
    {
        private readonly Regex durationRegex = new Regex(@"Duration:\s*([0-9][0-9]):([0-9][0-9]):([0-9][0-9])\.([0-9][0-9]),.*bitrate:\s*([0-9]+) kb\/s");
        private readonly Regex progressRegex = new Regex(@"size=\s*([0-9]+)kB\s+time=([0-9][0-9]):([0-9][0-9]):([0-9][0-9]).([0-9][0-9])\s+bitrate=\s*([0-9]+.[0-9])kbits\/s");

        private TimeSpan fileDuration;
        private TimeSpan actualConvertedDuration;

        private ProcessStartInfo ffmpegProcessStartInfo;

        private readonly List<FFMpegPass> ffmpegArgumentStringByPass = new List<FFMpegPass>();

        public ConversionJob_FFMPEG() : base()
        {
        }

        public ConversionJob_FFMPEG(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        public static VideoEncodingSpeed[] VideoEncodingSpeeds => new[]
           {
               VideoEncodingSpeed.UltraFast,
               VideoEncodingSpeed.SuperFast,
               VideoEncodingSpeed.VeryFast,
               VideoEncodingSpeed.Faster,
               VideoEncodingSpeed.Fast,
               VideoEncodingSpeed.Medium,
               VideoEncodingSpeed.Slow,
               VideoEncodingSpeed.Slower,
               VideoEncodingSpeed.VerySlow,
           };

        protected virtual string FfmpegPath
        {
            get
            {
                string applicationDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return System.IO.Path.Combine(applicationDirectory, "ffmpeg.exe");
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            this.ffmpegProcessStartInfo = null;

            string ffmpegPath = this.FfmpegPath;
            if (!System.IO.File.Exists(ffmpegPath))
            {
                this.ConversionFailed(Properties.Resources.ErrorCantFindFFMPEG);
                Diagnostics.Debug.Log($"Can't find ffmpeg executable ({ffmpegPath}). Try to reinstall the application.");
                return;
            }

            this.ffmpegProcessStartInfo = new ProcessStartInfo(ffmpegPath)
            {
                CreateNoWindow = true, 
                UseShellExecute = false, 
                RedirectStandardOutput = true, 
                RedirectStandardError = true
            };

            this.FillFFMpegArgumentsList();
        }

        protected virtual void FillFFMpegArgumentsList()
        {
            bool customCommandEnabled = this.ConversionPreset.GetSettingsValue<bool>(ConversionPreset.ConversionSettingKeys.EnableFFMPEGCustomCommand);
            if (customCommandEnabled)
            {
                // Custom command override other settings.
                string customCommand = this.ConversionPreset.GetSettingsValue<string>(ConversionPreset.ConversionSettingKeys.FFMPEGCustomCommand) ?? string.Empty;

                string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, customCommand);
                this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));

                return;
            }

            // This option are necessary to be able to read metadata on Windows. src: http://jonhall.info/how_to/create_id3_tags_using_ffmpeg
            const string MP3MetadataArgs = "-id3v2_version 3 -write_id3v1 1";

            // AAC have no standard tag system, use ApeV2 (that are compatible). src: http://eolindel.free.fr/foobar/tags.shtml
            const string AACMetadataArgs = "-write_apetag 1";

            switch (this.ConversionPreset.OutputType)
            {
                case OutputType.Aac:
                    {
                        string channelArgs = ConversionJob_FFMPEG.ComputeAudioChannelArgs(this.ConversionPreset);

                        // https://trac.ffmpeg.org/wiki/Encode/AAC
                        int audioEncodingBitrate = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);
                        string encoderArgs = $"-c:a aac -q:a {this.AACBitrateToQualityIndex(audioEncodingBitrate)} {channelArgs} {AACMetadataArgs}";

                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Avi:
                    {
                        // https://trac.ffmpeg.org/wiki/Encode/MPEG-4
                        int videoEncodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.VideoQuality);
                        int audioEncodingBitrate = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);

                        string transformArgs = ConversionJob_FFMPEG.ComputeTransformArgs(this.ConversionPreset);

                        string audioArgs = "-an";
                        if (this.ConversionPreset.GetSettingsValue<bool>(ConversionPreset.ConversionSettingKeys.EnableAudio))
                        {
                            audioArgs = string.Format("-c:a libmp3lame -qscale:a {0}", this.MP3VBRBitrateToQualityIndex(audioEncodingBitrate));
                        }

                        // Compute final arguments.
                        string videoFilteringArgs = ConversionJob_FFMPEG.Encapsulate("-vf", transformArgs);
                        string encoderArgs = $"-c:v mpeg4 -vtag xvid -qscale:v {this.MPEG4QualityToQualityIndex(videoEncodingQuality)} {audioArgs} {videoFilteringArgs} {MP3MetadataArgs}";
                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Flac:
                    {
                        string channelArgs = ConversionJob_FFMPEG.ComputeAudioChannelArgs(this.ConversionPreset);

                        // http://taer-naguur.blogspot.fr/2013/11/flac-audio-encoding-with-ffmpeg.html
                        string encoderArgs = $"-compression_level 12 {channelArgs}";
                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Gif:
                    {
                        // http://blog.pkh.me/p/21-high-quality-gif-with-ffmpeg.html
                        string fileName = Path.GetFileName(this.InputFilePath);
                        string tempPath = Path.GetTempPath();
                        string paletteFilePath = PathHelpers.GenerateUniquePath(tempPath + fileName + " - palette.png");

                        string transformArgs = ConversionJob_FFMPEG.ComputeTransformArgs(this.ConversionPreset);

                        // fps.
                        int framesPerSecond = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.VideoFramesPerSecond);
                        if (!string.IsNullOrEmpty(transformArgs))
                        {
                            transformArgs += ",";
                        }

                        transformArgs += string.Format("fps={0}", framesPerSecond);

                        // Generate palette.
                        string encoderArgs = string.Format("-vf \"{0},palettegen\"", transformArgs);
                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, paletteFilePath, encoderArgs);
                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass("Indexing colors", arguments, paletteFilePath));

                        // Create gif.
                        encoderArgs = string.Format("-i \"{0}\" -lavfi \"{1},paletteuse\"", paletteFilePath, transformArgs);
                        arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);
                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Ico:
                    {
                        string encoderArgs = string.Empty;
                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Jpg:
                    {
                        int encodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.ImageQuality);

                        float scaleFactor = this.ConversionPreset.GetSettingsValue<float>(ConversionPreset.ConversionSettingKeys.ImageScale);
                        string scaleArgs = string.Empty;
                        if (Math.Abs(scaleFactor - 1f) >= 0.005f)
                        {
                            scaleArgs = string.Format("-vf scale=iw*{0}:ih*{0}", scaleFactor.ToString("#.##", CultureInfo.InvariantCulture));
                        }

                        string encoderArgs = string.Format("-q:v {0} {1}", this.JPGQualityToQualityIndex(encodingQuality), scaleArgs);

                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Mp3:
                    {
                        string channelArgs = ConversionJob_FFMPEG.ComputeAudioChannelArgs(this.ConversionPreset);

                        string encoderArgs = string.Empty;
                        EncodingMode encodingMode = this.ConversionPreset.GetSettingsValue<EncodingMode>(ConversionPreset.ConversionSettingKeys.AudioEncodingMode);
                        int encodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);
                        switch (encodingMode)
                        {
                            case EncodingMode.Mp3VBR:
                                encoderArgs = $"-codec:a libmp3lame -q:a {this.MP3VBRBitrateToQualityIndex(encodingQuality)} {channelArgs} {MP3MetadataArgs}";
                                break;

                            case EncodingMode.Mp3CBR:
                                encoderArgs = $"-codec:a libmp3lame -b:a {encodingQuality}k {channelArgs} {MP3MetadataArgs}";
                                break;

                            default:
                                break;
                        }

                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Mkv:
                case OutputType.Mp4:
                    {
                        // https://trac.ffmpeg.org/wiki/Encode/H.264
                        // https://trac.ffmpeg.org/wiki/Encode/AAC
                        int videoEncodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.VideoQuality);
                        VideoEncodingSpeed videoEncodingSpeed = this.ConversionPreset.GetSettingsValue<VideoEncodingSpeed>(ConversionPreset.ConversionSettingKeys.VideoEncodingSpeed);
                        int audioEncodingBitrate = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);

                        string transformArgs = ConversionJob_FFMPEG.ComputeTransformArgs(this.ConversionPreset);
                        string videoFilteringArgs = ConversionJob_FFMPEG.Encapsulate("-vf", transformArgs);

                        string audioArgs = "-an";
                        if (this.ConversionPreset.GetSettingsValue<bool>(ConversionPreset.ConversionSettingKeys.EnableAudio))
                        {
                            audioArgs = $"-c:a aac -qscale:a {this.AACBitrateToQualityIndex(audioEncodingBitrate)}";
                        }

                        string encoderArgs = string.Format(
                            "-c:v libx264 -preset {0} -crf {1} {2} {3}",
                            this.H264EncodingSpeedToPreset(videoEncodingSpeed), 
                            this.H264QualityToCRF(videoEncodingQuality),
                            audioArgs, 
                            videoFilteringArgs);

                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Ogg:
                    {
                        string channelArgs = ConversionJob_FFMPEG.ComputeAudioChannelArgs(this.ConversionPreset);

                        int encodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);
                        string encoderArgs = $"-vn -codec:a libvorbis -qscale:a {this.OGGVBRBitrateToQualityIndex(encodingQuality)} {channelArgs}";
                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Ogv:
                    {
                        // https://trac.ffmpeg.org/wiki/TheoraVorbisEncodingGuide
                        int videoEncodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.VideoQuality);
                        int audioEncodingBitrate = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);

                        string transformArgs = ConversionJob_FFMPEG.ComputeTransformArgs(this.ConversionPreset);
                        string videoFilteringArgs = ConversionJob_FFMPEG.Encapsulate("-vf", transformArgs);

                        string audioArgs = "-an";
                        if (this.ConversionPreset.GetSettingsValue<bool>(ConversionPreset.ConversionSettingKeys.EnableAudio))
                        {
                            audioArgs = $"-codec:a libvorbis -qscale:a {this.OGGVBRBitrateToQualityIndex(audioEncodingBitrate)}";
                        }

                        string encoderArgs = $"-codec:v libtheora -qscale:v {this.OGVTheoraQualityToQualityIndex(videoEncodingQuality)} {audioArgs} {videoFilteringArgs}";

                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Png:
                    {
                        float scaleFactor = this.ConversionPreset.GetSettingsValue<float>(ConversionPreset.ConversionSettingKeys.ImageScale);
                        string scaleArgs = string.Empty;
                        if (Math.Abs(scaleFactor - 1f) >= 0.005f)
                        {
                            scaleArgs = string.Format("-vf scale=iw*{0}:ih*{0}", scaleFactor.ToString("#.##", CultureInfo.InvariantCulture));
                        }

                        // http://www.howtogeek.com/203979/is-the-png-format-lossless-since-it-has-a-compression-parameter/
                        string encoderArgs = string.Format("-compression_level 100 {0}", scaleArgs);

                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Wav:
                    {
                        string channelArgs = ConversionJob_FFMPEG.ComputeAudioChannelArgs(this.ConversionPreset);

                        EncodingMode encodingMode = this.ConversionPreset.GetSettingsValue<EncodingMode>(ConversionPreset.ConversionSettingKeys.AudioEncodingMode);
                        string encoderArgs = $"-acodec {this.WAVEncodingToCodecArgument(encodingMode)} {channelArgs}";
                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                case OutputType.Webm:
                    {
                        // https://trac.ffmpeg.org/wiki/Encode/VP9
                        int videoEncodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.VideoQuality);
                        int audioEncodingQuality = this.ConversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioBitrate);

                        string encodingArgs = string.Empty;
                        if (videoEncodingQuality == 63)
                        {
                            // Replace maximum quality settings by lossless compression.
                            encodingArgs = $"-lossless 1";
                        }
                        else
                        {
                            encodingArgs = $"-crf {this.WebmQualityToCRF(videoEncodingQuality)} -b:v 0";
                        }

                        string transformArgs = ConversionJob_FFMPEG.ComputeTransformArgs(this.ConversionPreset);
                        string videoFilteringArgs = ConversionJob_FFMPEG.Encapsulate("-vf", transformArgs);

                        string audioArgs = "-an";
                        if (this.ConversionPreset.GetSettingsValue<bool>(ConversionPreset.ConversionSettingKeys.EnableAudio))
                        {
                            audioArgs = string.Format("-c:a libvorbis -qscale:a {0}", this.OGGVBRBitrateToQualityIndex(audioEncodingQuality));
                        }

                        string encoderArgs = string.Format(
                            "-c:v libvpx-vp9 {0} {1} {2}", 
                            encodingArgs,
                            audioArgs, 
                            videoFilteringArgs);

                        string arguments = string.Format("-n -stats -i \"{0}\" {2} \"{1}\"", this.InputFilePath, this.OutputFilePath, encoderArgs);

                        this.ffmpegArgumentStringByPass.Add(new FFMpegPass(arguments));
                    }

                    break;

                default:
                    throw new NotImplementedException("Converter not implemented for output file type " +
                                                      this.ConversionPreset.OutputType);
            }

            if (this.ffmpegArgumentStringByPass.Count == 0)
            {
                throw new Exception("No ffmpeg arguments generated.");
            }

            for (int index = 0; index < this.ffmpegArgumentStringByPass.Count; index++)
            {
                if (string.IsNullOrEmpty(this.ffmpegArgumentStringByPass[index].Arguments))
                {
                    throw new Exception("Invalid ffmpeg process arguments.");
                }
            }
        }
        
        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            for (int index = 0; index < this.ffmpegArgumentStringByPass.Count; index++)
            {
                FFMpegPass currentPass = this.ffmpegArgumentStringByPass[index];

                this.UserState = currentPass.Name;
                this.ffmpegProcessStartInfo.Arguments = currentPass.Arguments;

                Diagnostics.Debug.Log($"Execute command: {this.ffmpegProcessStartInfo.FileName} {this.ffmpegProcessStartInfo.Arguments}.");
                Diagnostics.Debug.Log(string.Empty);

                try
                {
                    using (Process exeProcess = Process.Start(this.ffmpegProcessStartInfo))
                    {
                        using (StreamReader reader = exeProcess.StandardError)
                        {
                            while (!reader.EndOfStream)
                            {
                                if (this.CancelIsRequested && !exeProcess.HasExited)
                                {
                                    exeProcess.Kill();
                                }

                                string result = reader.ReadLine();

                                this.ParseFFMPEGOutput(result);

                                Diagnostics.Debug.Log($"ffmpeg output: {result}");
                            }
                        }

                        exeProcess.WaitForExit();
                    }
                }
                catch
                {
                    this.ConversionFailed(Properties.Resources.ErrorFailedToLaunchFFMPEG);
                    throw;
                }
            }

            Diagnostics.Debug.Log(string.Empty);

            // Clean intermediate files.
            for (int index = 0; index < this.ffmpegArgumentStringByPass.Count; index++)
            {
                FFMpegPass currentPass = this.ffmpegArgumentStringByPass[index];

                if (string.IsNullOrEmpty(currentPass.FileToDelete))
                {
                    continue;
                }

                Diagnostics.Debug.Log($"Delete intermediate file {currentPass.FileToDelete}.");

                File.Delete(currentPass.FileToDelete);
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

            if (this.fileDuration.Ticks > 0)
            {
                match = this.progressRegex.Match(input);
                if (match.Success && match.Groups.Count >= 7)
                {
                    int size = int.Parse(match.Groups[1].Value);
                    int hours = int.Parse(match.Groups[2].Value);
                    int minutes = int.Parse(match.Groups[3].Value);
                    int seconds = int.Parse(match.Groups[4].Value);
                    int milliseconds = int.Parse(match.Groups[5].Value) * 10;
                    float bitrate = 0f;
                    float.TryParse(match.Groups[6].Value, out bitrate);

                    this.actualConvertedDuration = new TimeSpan(0, hours, minutes, seconds, milliseconds);

                    this.Progress = this.actualConvertedDuration.Ticks / (float)this.fileDuration.Ticks;
                    return;
                }
            }

            // Remove file names from log to avoid false negative when some words like 'Error' are in file name (github issue #247).
            string inputWithoutFileNames = input.Replace(this.InputFilePath, string.Empty).Replace(this.OutputFilePath, string.Empty);

            if (inputWithoutFileNames.Contains("Exiting.") || inputWithoutFileNames.Contains("Error") || inputWithoutFileNames.Contains("Unsupported dimensions") || inputWithoutFileNames.Contains("No such file or directory"))
            {
                if (inputWithoutFileNames.StartsWith("Error while decoding stream") && inputWithoutFileNames.EndsWith("Invalid data found when processing input"))
                {
                    // It is normal for a transport stream to start with a broken frame.
                    // https://trac.ffmpeg.org/ticket/1622
                }
                else
                {
                    this.ConversionFailed(input);
                }
            }
        }

        private struct FFMpegPass
        {
            public string Name;
            public string Arguments;
            public string FileToDelete;

            public FFMpegPass(string name, string arguments, string fileToDelete)
            {
                this.Name = name;
                this.Arguments = arguments;
                this.FileToDelete = fileToDelete;
            }

            public FFMpegPass(string arguments)
            {
                this.Name = "Conversion";
                this.Arguments = arguments;
                this.FileToDelete = string.Empty;
            }
        }
    }
}
