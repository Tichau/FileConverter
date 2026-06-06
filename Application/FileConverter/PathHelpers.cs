// <copyright file="PathHelpers.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    using FileConverter.Diagnostics;

    public static class PathHelpers
    {
        private static Regex driveLetterRegex = new Regex(@"[a-zA-Z]:\\");
        private static Regex cdaTrackNumberRegex = new Regex(@"[a-zA-Z]:\\Track([0-9]+)\.cda");
        private static Regex pathRegex = new Regex(@"^(?:\\\\[^\\/:*?""<>|\r\n]+\\|[a-zA-Z]:\\)(?:[^\\/:*?""<>|\r\n]+\\)*[^\.\\/:*?""<>|\r\n][^\\/:*?""<>|\r\n]*$");
        private static Regex filenameRegex = new Regex(@"[^\\]*", RegexOptions.RightToLeft);
        private static Regex directoryRegex = new Regex(@"^(?<drive>\\\\[^\\/:*?""""<>|\r\n]+\\|[A-Za-z]:\\)(?:(?<folders>[^\\]*)\\)*");
        private static Regex dateRegex = new Regex(@"\(d:(?<format>[^)]*)\)");
        private static Regex sourceCreatedDateRegex = new Regex(@"\((?:sourcecreated|sc):(?<format>[^)]*)\)");
        private static Regex sourceModifiedDateRegex = new Regex(@"\((?:sourcemodified|sm):(?<format>[^)]*)\)");
        private static Regex formattedNumberIndexRegex = new Regex(@"\(n:i:(?<format>[^)]*)\)");
        private static Regex formattedNumberCountRegex = new Regex(@"\(n:c:(?<format>[^)]*)\)");
        private static readonly HashSet<string> ReservedDeviceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };

        public static bool IsPathDriveLetterValid(string path)
        {
            return PathHelpers.driveLetterRegex.IsMatch(path);
        }

        public static string GetPathDriveLetter(string path)
        {
            return PathHelpers.driveLetterRegex.Match(path).Groups[0].Value;
        }

        public static bool IsOnCDDrive(string path)
        {
            string pathDriveLetter = GetPathDriveLetter(path);
            if (string.IsNullOrEmpty(pathDriveLetter))
            {
                return false;
            }

            char driveLetter = pathDriveLetter[0];

            char[] driveLetters = Ripper.CDDrive.GetCDDriveLetters();
            for (int index = 0; index < driveLetters.Length; index++)
            {
                if (driveLetters[index] == driveLetter)
                {
                    return true;
                }
            }

            return false;
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

        public static bool TryNormalizeGeneratedPath(string path, out string normalizedPath, out string errorMessage)
        {
            normalizedPath = path;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "The generated output path is empty.";
                return false;
            }

            if (!PathHelpers.IsPathValid(path))
            {
                errorMessage = "The generated output path is not a valid absolute Windows path.";
                return false;
            }

            if (PathHelpers.ContainsRelativeDirectorySegment(path))
            {
                errorMessage = "The generated output path contains a relative directory segment.";
                return false;
            }

            try
            {
                normalizedPath = System.IO.Path.GetFullPath(path);
            }
            catch (Exception exception)
            {
                errorMessage = $"The generated output path could not be normalized: {exception.Message}";
                return false;
            }

            if (!PathHelpers.IsPathValid(normalizedPath))
            {
                errorMessage = "The normalized output path is not valid.";
                return false;
            }

            if (PathHelpers.ContainsReservedDeviceName(normalizedPath))
            {
                errorMessage = "The generated output path contains a reserved Windows device name.";
                return false;
            }

            return true;
        }

        public static string GetExtensionWithoutDot(string path)
        {
            string extension = System.IO.Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension) || extension.Length <= 1)
            {
                return string.Empty;
            }

            return extension.Substring(1).ToLowerInvariant();
        }

        public static string GetFileName(string path)
        {
            MatchCollection matchCollection = PathHelpers.filenameRegex.Matches(path);
            Match filenameMatch = matchCollection.Count > 0 ? matchCollection[0] : null;
            return filenameMatch?.Groups[0].Value;
        }

        public static string GetDrive(string path)
        {
            MatchCollection matchCollection = PathHelpers.directoryRegex.Matches(path);
            Match match = matchCollection.Count > 0 ? matchCollection[0] : null;

            Group matchGroup = match?.Groups["drive"];
            return matchGroup?.Captures[0].Value;
        }

        public static IEnumerable<string> GetDirectories(string path)
        {
            MatchCollection matchCollection = PathHelpers.directoryRegex.Matches(path);
            Match match = matchCollection.Count > 0 ? matchCollection[0] : null;

            Group matchGroup = match?.Groups["folders"];
            if (matchGroup == null)
            {
                yield break;
            }

            for (int index = 0; index < matchGroup.Captures.Count; index++)
            {
                yield return matchGroup.Captures[index].Value;
            }
        }

        public static string GenerateUniquePath(string path, params string[] blacklist)
        {
            string baseExtension = System.IO.Path.GetExtension(path);
            string basePath = path.Substring(0, path.Length - baseExtension.Length);
            int index = 2;
            while (System.IO.File.Exists(path) ||
                (blacklist != null && System.Array.Exists(blacklist, match => match == path)))
            {
                path = $"{basePath} ({index}){baseExtension}";
                index++;
            }

            return path;
        }

        public static string GenerateTemporaryFilePath(string preferredFileName)
        {
            string safeFileName = SanitizeFileSystemToken(System.IO.Path.GetFileName(preferredFileName));
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = "conversion.tmp";
            }

            string tempFolder = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ZFileConverter",
                Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempFolder);

            return System.IO.Path.Combine(tempFolder, safeFileName);
        }

        public static bool CreateFolders(string filePath)
        {
            // Create output folders that doesn't already exist.
            StringBuilder path = new StringBuilder(filePath.Length);
            string drive = PathHelpers.GetDrive(filePath);
            path.Append(drive);

            foreach (string directory in PathHelpers.GetDirectories(filePath))
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
                        Debug.Log($"Can't create directories for path {filePath}");
                        return false;
                    }
                }
            }

            return true;
        }

        public static string GenerateFilePathFromTemplate(
            string inputFilePath,
            OutputType outputFileExtension,
            string outputFilePathTemplate,
            int numberIndex,
            int numberMax,
            string presetName = null,
            string presetFullName = null)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                return "Invalid input file path (argument 0).";
            }

            string inputExtension = GetExtensionWithoutDot(inputFilePath);
            string inputPathWithoutExtension = inputFilePath;
            if (!string.IsNullOrEmpty(inputExtension))
            {
                inputPathWithoutExtension = inputFilePath.Substring(0, inputFilePath.Length - inputExtension.Length - 1);
            }

            string outputExtension = outputFileExtension.ToString().ToLowerInvariant();

            if (string.IsNullOrEmpty(outputFilePathTemplate))
            {
                // Default output path.
                return inputPathWithoutExtension + "." + outputExtension;
            }

            string fileName = System.IO.Path.GetFileName(inputPathWithoutExtension);
            string parentDirectory = System.IO.Path.GetDirectoryName(inputPathWithoutExtension);
            if (string.IsNullOrEmpty(parentDirectory))
            {
                parentDirectory = System.Environment.CurrentDirectory;
            }

            if (!parentDirectory.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
            {
                parentDirectory += System.IO.Path.DirectorySeparatorChar;
            }

            string[] directories = parentDirectory.Substring(0, parentDirectory.Length - 1).Split(System.IO.Path.DirectorySeparatorChar);

            // Generate output path from template.
            string outputPath = outputFilePathTemplate;

            outputPath = outputPath.Replace("(path)", parentDirectory);
            outputPath = outputPath.Replace("(p)", parentDirectory);

            outputPath = outputPath.Replace("(filename)", fileName);
            outputPath = outputPath.Replace("(f)", fileName);
            outputPath = outputPath.Replace("(F)", fileName.ToUpperInvariant());

            outputPath = outputPath.Replace("(outputext)", outputExtension);
            outputPath = outputPath.Replace("(o)", outputExtension);
            outputPath = outputPath.Replace("(O)", outputExtension.ToUpperInvariant());

            outputPath = outputPath.Replace("(inputext)", inputExtension);
            outputPath = outputPath.Replace("(i)", inputExtension);
            outputPath = outputPath.Replace("(I)", inputExtension.ToUpperInvariant());

            string myDocumentsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "\\";
            outputPath = outputPath.Replace("(p:d)", myDocumentsFolder);
            outputPath = outputPath.Replace("(p:documents)", myDocumentsFolder);

            string myMusicFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic) + "\\";
            outputPath = outputPath.Replace("(p:m)", myMusicFolder);
            outputPath = outputPath.Replace("(p:music)", myMusicFolder);

            string myVideoFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos) + "\\";
            outputPath = outputPath.Replace("(p:v)", myVideoFolder);
            outputPath = outputPath.Replace("(p:videos)", myVideoFolder);

            string myPictureFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures) + "\\";
            outputPath = outputPath.Replace("(p:p)", myPictureFolder);
            outputPath = outputPath.Replace("(p:pictures)", myPictureFolder);

            for (int index = 0; index < directories.Length; index++)
            {
                outputPath = outputPath.Replace($"(d{directories.Length - index - 1})", directories[index]);
                outputPath = outputPath.Replace($"(D{directories.Length - index - 1})", directories[index].ToUpperInvariant());
            }

            outputPath = outputPath.Replace("(n:i)", numberIndex.ToString());
            outputPath = outputPath.Replace("(n:c)", numberMax.ToString());

            outputPath = formattedNumberIndexRegex.Replace(outputPath, match => FormatNumber(numberIndex, match.Groups["format"].Value));
            outputPath = formattedNumberCountRegex.Replace(outputPath, match => FormatNumber(numberMax, match.Groups["format"].Value));

            string safePresetName = SanitizeFileSystemToken(presetName);
            string safePresetPath = SanitizePresetPath(presetFullName ?? presetName);
            outputPath = outputPath.Replace("(preset)", safePresetName);
            outputPath = outputPath.Replace("(presetname)", safePresetName);
            outputPath = outputPath.Replace("(presetpath)", safePresetPath);

            outputPath = dateRegex.Replace(outputPath, match => FormatDate(DateTime.Now, match.Groups["format"].Value));
            outputPath = sourceCreatedDateRegex.Replace(outputPath, match => FormatDate(GetCreationTime(inputFilePath), match.Groups["format"].Value));
            outputPath = sourceModifiedDateRegex.Replace(outputPath, match => FormatDate(GetLastWriteTime(inputFilePath), match.Groups["format"].Value));

            outputPath += "." + outputExtension;

            return outputPath;
        }

        private static string FormatNumber(int number, string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return number.ToString(NumberFormatInfo.InvariantInfo);
            }

            return number.ToString(format, NumberFormatInfo.InvariantInfo);
        }

        private static string FormatDate(DateTime date, string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return date.ToString(CultureInfo.InvariantCulture).Replace('/', '-').Replace(':', '\'');
            }

            return date.ToString(format, CultureInfo.InvariantCulture).Replace('/', '-').Replace(':', '\'');
        }

        private static DateTime GetCreationTime(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return DateTime.Now;
            }

            return System.IO.File.GetCreationTime(path);
        }

        private static DateTime GetLastWriteTime(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return DateTime.Now;
            }

            return System.IO.File.GetLastWriteTime(path);
        }

        private static bool ContainsRelativeDirectorySegment(string path)
        {
            string[] segments = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < segments.Length; index++)
            {
                if (segments[index] == "." || segments[index] == "..")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsReservedDeviceName(string path)
        {
            string root = System.IO.Path.GetPathRoot(path);
            string pathWithoutRoot = string.IsNullOrEmpty(root) ? path : path.Substring(root.Length);
            string[] segments = pathWithoutRoot.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int index = 0; index < segments.Length; index++)
            {
                string segment = segments[index].TrimEnd(' ', '.');
                string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(segment);
                if (ReservedDeviceNames.Contains(nameWithoutExtension))
                {
                    return true;
                }
            }

            return false;
        }

        private static string SanitizePresetPath(string presetPath)
        {
            if (string.IsNullOrEmpty(presetPath))
            {
                return string.Empty;
            }

            string[] segments = presetPath.Split('/');
            for (int index = 0; index < segments.Length; index++)
            {
                segments[index] = SanitizeFileSystemToken(segments[index]);
            }

            return string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), segments);
        }

        private static string SanitizeFileSystemToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(value.Length);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                builder.Append(Array.IndexOf(invalidFileNameChars, character) >= 0 ? '_' : character);
            }

            return builder.ToString();
        }
    }
}
