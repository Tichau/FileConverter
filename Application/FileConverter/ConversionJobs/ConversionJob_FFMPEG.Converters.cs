// <copyright file="ConversionJob_FFMPEG.Converters.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.Globalization;

    using FileConverter.Controls;

    public partial class ConversionJob_FFMPEG
    {
        private static string Encapsulate(string optionName, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return string.Empty;
            }

            return string.Format("{0} \"{1}\"", optionName, args);
        }

        /// <summary>
        /// Compute the argument needed to change the number of channels in an audio file.
        /// </summary>
        /// <param name="conversionPreset">The conversion preset</param>
        /// <returns>The argument string.</returns>
        /// https://trac.ffmpeg.org/wiki/AudioChannelManipulation
        private static string ComputeAudioChannelArgs(ConversionPreset conversionPreset)
        {
            string channelArgs = string.Empty;
            int channelCount = conversionPreset.GetSettingsValue<int>(ConversionPreset.ConversionSettingKeys.AudioChannelCount);
            if (channelCount > 0)
            {
                channelArgs = $"-ac {channelCount}";
            }

            return channelArgs;
        }

        private static string ComputeTransformArgs(ConversionPreset conversionPreset)
        {
            float scaleFactor = conversionPreset.GetSettingsValue<float>(ConversionPreset.ConversionSettingKeys.VideoScale);
            string scaleArgs = string.Empty;

            if (conversionPreset.OutputType == OutputType.Mkv || conversionPreset.OutputType == OutputType.Mp4)
            {
                // This presets use h264 codec, the size of the video need to be divisible by 2.
                scaleArgs = string.Format("scale=trunc(iw*{0}/2)*2:trunc(ih*{0}/2)*2", scaleFactor.ToString("#.##", CultureInfo.InvariantCulture));
            }
            else if (Math.Abs(scaleFactor - 1f) >= 0.005f)
            {
                scaleArgs = string.Format("scale=iw*{0}:ih*{0}", scaleFactor.ToString("#.##", CultureInfo.InvariantCulture));
            }

            float rotationAngleInDegrees = conversionPreset.GetSettingsValue<float>(ConversionPreset.ConversionSettingKeys.VideoRotation);
            string rotationArgs = string.Empty;
            if (Math.Abs(rotationAngleInDegrees - 0f) >= 0.05f)
            {
                // Transpose:
                // 0: 90 CounterClockwise and vertical flip
                // 1: 90 Clockwise
                // 2: 90 CounterClockwise 
                // 3: 90 Clockwise and vertical flip
                if (Math.Abs(rotationAngleInDegrees - 90f) <= 0.05f)
                {
                    rotationArgs = "transpose=2";
                }
                else if (Math.Abs(rotationAngleInDegrees - 180f) <= 0.05f)
                {
                    rotationArgs = "vflip,hflip";
                }
                else if (Math.Abs(rotationAngleInDegrees - 270f) <= 0.05f)
                {
                    rotationArgs = "transpose=1";
                }
                else
                {
                    Diagnostics.Debug.LogError("Unsupported rotation: {0}°", rotationAngleInDegrees);
                }
            }

            // Build vf args content (scale=..., transpose=...)
            string transformArgs = string.Empty;
            if (!string.IsNullOrEmpty(scaleArgs))
            {
                transformArgs += scaleArgs;
            }

            if (!string.IsNullOrEmpty(rotationArgs))
            {
                if (!string.IsNullOrEmpty(transformArgs))
                {
                    transformArgs += ",";
                }

                transformArgs += rotationArgs;
            }

            if (conversionPreset.OutputType == OutputType.Mkv || conversionPreset.OutputType == OutputType.Mp4)
            {
                // http://trac.ffmpeg.org/wiki/Encode/H.264#Encodingfordumbplayers
                // YUV planar color space with 4:2:0 chroma subsampling
                transformArgs += (transformArgs.Length > 0 ? "," : "") + "format=yuv420p";
                // maybe there should be an option for this on the settings?
            }

            return transformArgs;
        }

        /// <summary>
        /// Convert bitrate in <c>mp3</c> encoder quality index.
        /// </summary>
        /// <param name="bitrate">Wanted bitrate value.</param>
        /// <returns>The <c>mp3</c> encoder quality index associated to the given bitrate value.</returns>
        /// https://trac.ffmpeg.org/wiki/Encode/MP3
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

            throw new Exception("Unknown Ogg VBR bitrate.");
        }

        /// <summary>
        /// Convert video quality index to lib <c>theora</c> video quality level.
        /// </summary>
        /// <param name="quality">The quality index.</param>
        /// <returns>Returns the video quality index.</returns>
        /// The range of the video quality level is 0-10: where 10 is the highest quality/largest filesize, 0 being the lowest quality/smallest filesize.
        /// https://trac.ffmpeg.org/wiki/TheoraVorbisEncodingGuide
        private int OGVTheoraQualityToQualityIndex(int quality)
        {
            return quality;
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
        /// Convert video quality index to MPEG4 video quality level.
        /// </summary>
        /// <param name="quality">The quality index.</param>
        /// <returns>Returns the H264 constant rate factor.</returns>
        /// The range of the video quality level is 1-31: where 1 is the highest quality/largest filesize, 31 being the lowest quality/smallest filesize.
        /// https://trac.ffmpeg.org/wiki/Encode/MPEG-4
        private int MPEG4QualityToQualityIndex(int quality)
        {
            return 31 - quality;
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
        /// Convert image quality index to JPG quality index.
        /// </summary>
        /// <param name="quality">The quality index.</param>
        /// <returns>Returns the JPG quality index.</returns>
        /// The range of the quantizer scale is 1-31: where 1 is better quality, and 31 is worst possible. 
        /// http://superuser.com/questions/318845/improve-quality-of-ffmpeg-created-jpgs
        private int JPGQualityToQualityIndex(int quality)
        {
            return 31 - quality;
        }

        /// <summary>
        /// Convert video encoding speed to H264 preset.
        /// </summary>
        /// <param name="encodingSpeed">The encoding speed.</param>
        /// <returns>The H264 preset.</returns>
        private string H264EncodingSpeedToPreset(VideoEncodingSpeed encodingSpeed)
        {
            switch (encodingSpeed)
            {
                case VideoEncodingSpeed.UltraFast:
                    return "ultrafast";

                case VideoEncodingSpeed.SuperFast:
                    return "superfast";

                case VideoEncodingSpeed.VeryFast:
                    return "veryfast";

                case VideoEncodingSpeed.Faster:
                    return "faster";

                case VideoEncodingSpeed.Fast:
                    return "fast";

                case VideoEncodingSpeed.Medium:
                    return "medium";

                case VideoEncodingSpeed.Slow:
                    return "slow";

                case VideoEncodingSpeed.Slower:
                    return "slower";

                case VideoEncodingSpeed.VerySlow:
                    return "veryslow";
            }

            throw new Exception("Unknown H264 encoding speed.");
        }

        /// <summary>
        /// Convert bitrate in <c>aac</c> encoder quality index.
        /// </summary>
        /// <param name="bitrate">Wanted bitrate value.</param>
        /// <returns>The <c>aac</c> encoder quality index associated to the given bitrate value.</returns>
        /// See the file Resources/FFMPEG retro engineering.ods for details.
        private string AACBitrateToQualityIndex(int bitrate)
        {
            switch (bitrate)
            {
                case 460:
                    return "3.9";

                case 340:
                    return "3";

                case 256:
                    return "2.2";

                case 224:
                    return "1.9";

                case 192:
                    return "1.6";

                case 155:
                    return "1.3";

                case 128:
                    return "1";

                case 112:
                    return "0.9";

                case 96:
                    return "0.75";

                case 80:
                    return "0.6";

                case 64:
                    return "0.45";

                case 48:
                    return "0.3";

                case 32:
                    return "0.2";

                case 16:
                    return "0.1";
            }

            throw new Exception("Unknown VBR bitrate.");
        }

        /// <summary>
        /// Convert video quality index to H264 constant rate factor.
        /// </summary>
        /// <param name="quality">The quality index.</param>
        /// <returns>Returns the H264 constant rate factor.</returns>
        /// The range of the quantizer scale is 0-63: lower values mean better quality.
        /// https://trac.ffmpeg.org/wiki/Encode/VP9
        private int WebmQualityToCRF(int quality)
        {
            return 63 - quality;
        }
    }
}
