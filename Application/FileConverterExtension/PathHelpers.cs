// <copyright file="FileConverterExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System.IO;
    using System;
    using Microsoft.Win32;

    public static class PathHelpers
    {
        private static RegistryKey fileConverterRegistryKey;
        private static string fileConverterPath;

        public static string UserSettingsFilePath => Path.Combine(PathHelpers.GetUserDataFolderPath, "Settings.user.xml");

        public static string DefaultSettingsFilePath
        {
            get
            {
                string pathToFileConverterExecutable = PathHelpers.FileConverterPath;
                if (string.IsNullOrEmpty(pathToFileConverterExecutable))
                {
                    return null;
                }

                return Path.Combine(Path.GetDirectoryName(pathToFileConverterExecutable), "Settings.default.xml");
            }
        }

        public static RegistryKey FileConverterRegistryKey
        {
            get
            {
                if (PathHelpers.fileConverterRegistryKey == null)
                {
                    PathHelpers.fileConverterRegistryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
                }

                return PathHelpers.fileConverterRegistryKey;
            }
        }

        public static string FileConverterPath
        {
            get
            {
                if (string.IsNullOrEmpty(PathHelpers.fileConverterPath))
                {
                    RegistryKey registryKey = PathHelpers.FileConverterRegistryKey;
                    if (registryKey == null)
                    {
                        return null;
                    }

                    PathHelpers.fileConverterPath = registryKey.GetValue("Path") as string;
                }

                return PathHelpers.fileConverterPath;
            }
        }

        public static string GetUserDataFolderPath
        {
            get
            {
                string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                path = Path.Combine(path, "FileConverter");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }
    }
}
