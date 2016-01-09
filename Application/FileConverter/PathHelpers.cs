// <copyright file="PathHelpers.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public static class PathHelpers
    {
        private static Regex driveLetterRegex = new Regex(@"[a-zA-Z]:\\");
        private static Regex cdaTrackNumberRegex = new Regex(@"[a-zA-Z]:\\Track([0-9]+)\.cda");
        private static Regex pathRegex = new Regex(@"^[a-zA-Z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\.\\/:*?""<>|\r\n][^\\/:*?""<>|\r\n]*$");
        private static Regex filenameRegex = new Regex(@"[^\\]*", RegexOptions.RightToLeft);
        private static Regex directoryRegex = new Regex(@"(?:([^\\]*)\\)*");

        public static bool IsPathDriveLetterValid(string path)
        {
            return PathHelpers.driveLetterRegex.IsMatch(path);
        }

        public static string GetPathDriveLetter(string path)
        {
            return PathHelpers.driveLetterRegex.Match(path).Groups[0].Value;
        }

        public static int GetCDATrackNumber(string path)
        {
            Match match = PathHelpers.cdaTrackNumberRegex.Match(path);
            string stringNumber = match.Groups[1].Value;
            return int.Parse(stringNumber);
        }

        public static bool IsPathValid(string path)
        {
            return PathHelpers.pathRegex.IsMatch(path);
        }

        public static string GetFileName(string path)
        {
            MatchCollection matchCollection = PathHelpers.filenameRegex.Matches(path);
            Match filenameMatch = matchCollection.Count > 0 ? matchCollection[0] : null;
            return filenameMatch?.Groups[0].Value;
        }
        
        public static IEnumerable<string> GetDirectories(string path)
        {
            MatchCollection matchCollection = PathHelpers.directoryRegex.Matches(path);
            Match match = matchCollection.Count > 0 ? matchCollection[0] : null;
            if (match == null || match.Groups.Count < 2)
            {
                yield break;
            }

            Group matchGroup = match.Groups[1];
            for (int index = 0; index < matchGroup.Captures.Count; index++)
            {
                yield return matchGroup.Captures[index].Value;
            }
        }

        public static string GenerateUniquePath(string path)
        {
            string baseExtension = System.IO.Path.GetExtension(path);
            string basePath = path.Substring(0, path.Length - baseExtension.Length);
            int index = 2;
            while (System.IO.File.Exists(path))
            {
                path = string.Format("{0} ({1}){2}", basePath, index, baseExtension);
                index++;
            }

            return path;
        }

        public static string GetExtensionCategory(string extension)
        {
            switch (extension)
            {
                case "aac":
                case "aiff":
                case "ape":
                case "cda":
                case "flac":
                case "mp3":
                case "m4a":
                case "ogg":
                case "wav":
                case "wma":
                    return InputCategoryNames.Audio;

                case "avi":
                case "bik":
                case "3gp":
                case "flv":
                case "mp4":
                case "mov":
                case "mkv":
                case "webm":
                case "wmv":
                    return InputCategoryNames.Video;

                case "bmp":
                case "ico":
                case "jpg":
                case "jpeg":
                case "png":
                case "tiff":
                    return InputCategoryNames.Image;
            }

            return InputCategoryNames.Misc;
        }

        public static bool IsOutputTypeCompatibleWithCategory(OutputType outputType, string category)
        {
            if (category == InputCategoryNames.Misc)
            {
                // Misc category contains unsorted input extensions, so we consider that they are compatible to be tolerant.
                return true;
            }

            switch (outputType)
            {
                case OutputType.Aac:
                case OutputType.Flac:
                case OutputType.Mp3:
                case OutputType.Ogg:
                case OutputType.Wav:
                    return category == InputCategoryNames.Audio || category == InputCategoryNames.Video;

                case OutputType.Avi:
                case OutputType.Mkv:
                    return category == InputCategoryNames.Video;
                    
                case OutputType.Ico:
                case OutputType.Png:
                case OutputType.Jpg:
                    return category == InputCategoryNames.Image;

                default:
                    return false;
            }
        }

        public static string GetUserDataFolderPath()
        {
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            path = System.IO.Path.Combine(path, "FileConverter");

            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            return path;
        }

        public static class InputCategoryNames
        {
            public const string Audio = "Audio";
            public const string Video = "Video";
            public const string Image = "Image";
            public const string Misc = "Misc";
        }
    }
}
