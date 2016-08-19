// <copyright file="PathHelpers.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using FileConverter.Diagnostics;

    public static class PathHelpers
    {
        private static Regex driveLetterRegex = new Regex(@"[a-zA-Z]:\\");
        private static Regex cdaTrackNumberRegex = new Regex(@"[a-zA-Z]:\\Track([0-9]+)\.cda");
        private static Regex pathRegex = new Regex(@"^(?:\\\\[^\\/:*?""<>|\r\n]+\\|[a-zA-Z]:\\)(?:[^\\/:*?""<>|\r\n]+\\)*[^\.\\/:*?""<>|\r\n][^\\/:*?""<>|\r\n]*$");
        private static Regex filenameRegex = new Regex(@"[^\\]*", RegexOptions.RightToLeft);
        private static Regex directoryRegex = new Regex(@"^(?<drive>\\\\[^\\/:*?""""<>|\r\n]+\\|[A-Za-z]:\\)(?:(?<folders>[^\\]*)\\)*");

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
                        Debug.Log(string.Format("Can't create directories for path {0}", filePath));
                        return false;
                    }
                }
            }

            return true;
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

        public static string GenerateFilePathFromTemplate(string inputFilePath, OutputType outputFileExtension, string outputFilePathTemplate, int numberIndex, int numberMax)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                return "Invalid input file path (argument 0).";
            }

            string inputExtension = System.IO.Path.GetExtension(inputFilePath).Substring(1);
            string inputPathWithoutExtension = inputFilePath.Substring(0, inputFilePath.Length - inputExtension.Length - 1);
            string outputExtension = outputFileExtension.ToString().ToLowerInvariant();

            if (string.IsNullOrEmpty(outputFilePathTemplate))
            {
                // Default output path.
                return inputPathWithoutExtension + "." + outputExtension;
            }

            string fileName = System.IO.Path.GetFileName(inputPathWithoutExtension);
            string parentDirectory = System.IO.Path.GetDirectoryName(inputPathWithoutExtension);
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
                outputPath = outputPath.Replace(string.Format("(d{0})", directories.Length - index - 1), directories[index]);
                outputPath = outputPath.Replace(string.Format("(D{0})", directories.Length - index - 1), directories[index].ToUpperInvariant());
            }

            outputPath = outputPath.Replace("(n:i)", numberIndex.ToString());
            outputPath = outputPath.Replace("(n:c)", numberMax.ToString());

            outputPath += "." + outputExtension;

            return outputPath;
        }
    }
}
