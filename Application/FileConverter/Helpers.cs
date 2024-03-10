// <copyright file="Helpers.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    using FileConverter.ConversionJobs;
    using FileConverter.Services;

    using SharpShell.Helpers;

    using Microsoft.Win32;
    using CommunityToolkit.Mvvm.DependencyInjection;

    public static class Helpers
    {
        public static readonly string[] CompatibleInputExtensions = {
            "3gp","3gpp","aac","aiff","ape","arw","avi","bik","bmp","cda","cr2","dds","dng","doc","docx",
            "exr","flac","flv","gif","heic","ico","jfif","jpg","jpeg","m4a","m4b","m4v","mkv","mov","mp3","mp4",
            "mpg","mpeg","nef","odp","ods","odt","oga","ogg","ogv","opus","pdf","png","ppt","pptx","psd",
            "raf", "rm","svg","tga","tif","tiff", "ts", "vob","wav","webm","webp","wma","wmv","xls","xlsx"
        };

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
                case "m4b":
                case "oga":
                case "ogg":
                case "opus":
                case "wav":
                case "wma":
                    return InputCategoryNames.Audio;

                case "3gp":
                case "3gpp":
                case "avi":
                case "bik":
                case "flv":
                case "m4v":
                case "mp4":
                case "mpg":
                case "mpeg":
                case "mov":
                case "mkv":
                case "ogv":
                case "rm":
                case "ts":
                case "vob":
                case "webm":
                case "wmv":
                    return InputCategoryNames.Video;

                case "arw":
                case "bmp":
                case "cr2":
                case "dds":
                case "dng":
                case "exr":
                case "heic":
                case "ico":
                case "jfif":
                case "jpg":
                case "jpeg":
                case "nef":
                case "png":
                case "psd":
                case "raf":
                case "tga":
                case "tif":
                case "tiff":
                case "svg":
                case "xcf":
                case "webp":
                    return InputCategoryNames.Image;

                case "gif":
                    return InputCategoryNames.AnimatedImage;

                case "pdf":
                case "doc":
                case "docx":
                case "ppt":
                case "pptx":
                case "odp":
                case "ods":
                case "odt":
                case "xls":
                case "xlsx":
                    return InputCategoryNames.Document;
            }

            return InputCategoryNames.Misc;
        }

        public static bool RegisterShellExtension(string shellExtensionPath)
        {
            if (!Application.IsInAdmininstratorPrivileges)
            {
                Diagnostics.Debug.LogError("File Converter needs administrator privileges to register the shell extension.");
                return false;
            }

            if (!File.Exists(shellExtensionPath))
            {
                Diagnostics.Debug.LogError($"Shell extension {shellExtensionPath} does not exists.");
                return false;
            }

            Diagnostics.Debug.Log($"Install and register shell extension: {shellExtensionPath}.");

            var regasm = new RegAsm();
            var success = regasm.Register64(shellExtensionPath, true);
            if (success)
            {
                Diagnostics.Debug.Log($"{shellExtensionPath} installed and registered.");
                Diagnostics.Debug.Log(regasm.StandardOutput);
                return true;
            }
            else
            {
                Diagnostics.Debug.LogError(errorCode: 0x05, $"{shellExtensionPath} failed to register.");
                Diagnostics.Debug.LogError(regasm.StandardError);
                return false;
            }
        }

        public static bool UnregisterExtension(string shellExtensionPath)
        {
            if (!Application.IsInAdmininstratorPrivileges)
            {
                Diagnostics.Debug.LogError("File Converter needs administrator privileges to unregister the shell extension.");
                return false;
            }

            if (!File.Exists(shellExtensionPath))
            {
                Diagnostics.Debug.LogError($"Shell extension {shellExtensionPath} does not exists.");
                return false;
            }

            Diagnostics.Debug.Log($"Unregister and uninstall shell extension: {shellExtensionPath}.");

            var regasm = new RegAsm();
            var success = regasm.Unregister64(shellExtensionPath);
            if (success)
            {
                Diagnostics.Debug.Log($"{shellExtensionPath} uninstalled.");
                Diagnostics.Debug.Log(regasm.StandardOutput);
                return true;
            }
            else
            {
                Diagnostics.Debug.LogError(errorCode: 0x05, $"{shellExtensionPath} failed to uninstall.");
                Diagnostics.Debug.LogError(regasm.StandardError);
                return false;
            }
        }

        public static IEnumerable<CultureInfo> GetSupportedCultures()
        {
            // Get all cultures.
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            // Find the location where application installed.
            string exeLocation = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));

            // Return all culture for which satellite folder found with culture code.
            foreach (CultureInfo cultureInfo in cultures)
            {
                if (!string.IsNullOrEmpty(cultureInfo.Name) && Directory.Exists(Path.Combine(exeLocation, "Languages", cultureInfo.Name)))
                {
                    yield return cultureInfo;
                }
            }
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
                case OutputType.Mp4:
                case OutputType.Ogv:
                case OutputType.Webm:
                    return category == InputCategoryNames.Video || category == InputCategoryNames.AnimatedImage;

                case OutputType.Ico:
                case OutputType.Jpg:
                case OutputType.Png:
                case OutputType.Webp:
                    return category == InputCategoryNames.Image || category == InputCategoryNames.Document;

                case OutputType.Gif:
                    return category == InputCategoryNames.Image || category == InputCategoryNames.Video || category == InputCategoryNames.AnimatedImage;

                case OutputType.Pdf:
                    return category == InputCategoryNames.Image || category == InputCategoryNames.Document;

                default:
                    return false;
            }
        }

        public static Thread InstantiateThread(string name, ThreadStart threadStart)
        {
            ISettingsService settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
            CultureInfo currentCulture = settingsService?.Settings?.ApplicationLanguage;

            Thread thread = new Thread(threadStart);
            thread.Name = name;

            if (currentCulture != null)
            {
                thread.CurrentCulture = currentCulture;
                thread.CurrentUICulture = currentCulture;
            }

            return thread;
        }

        public static Thread InstantiateThread(string name, ParameterizedThreadStart parameterizedThreadStart)
        {
            ISettingsService settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
            CultureInfo currentCulture = settingsService?.Settings?.ApplicationLanguage;

            Thread thread = new Thread(parameterizedThreadStart);
            thread.Name = name;

            if (currentCulture != null)
            {
                thread.CurrentCulture = currentCulture;
                thread.CurrentUICulture = currentCulture;
            }

            return thread;
        }

        /// <summary>
        /// Check whether Microsoft office is available or not.
        /// </summary>
        /// <param name="application">The office application name.</param>
        /// <returns>Returns true if Office is installed on the computer.</returns>
        /// source: http://stackoverflow.com/questions/3266675/how-to-detect-installed-version-of-ms-office/3267832#3267832
        /// source: http://www.codeproject.com/Articles/26520/Getting-Office-s-Version
        public static bool IsMicrosoftOfficeApplicationAvailable(ConversionJobs.ConversionJob_Office.ApplicationName application)
        {
            string registryKeyPattern = @"Software\Microsoft\Windows\CurrentVersion\App Paths\";
            switch (application)
            {
                case ConversionJob_Office.ApplicationName.Word:
                    registryKeyPattern += "winword.exe";
                    break;

                case ConversionJob_Office.ApplicationName.PowerPoint:
                    registryKeyPattern += "powerpnt.exe";
                    break;

                case ConversionJob_Office.ApplicationName.Excel:
                    registryKeyPattern += "excel.exe";
                    break;

                case ConversionJob_Office.ApplicationName.None:
                    return false;
            }

            // Looks inside CURRENT_USER.
            RegistryKey winwordKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryKeyPattern, false);
            if (winwordKey != null)
            {
                string winwordPath = winwordKey.GetValue(string.Empty).ToString();
                if (!string.IsNullOrEmpty(winwordPath))
                {
                    return true;
                }
            }

            // If not found, looks inside LOCAL_MACHINE.
            winwordKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryKeyPattern, false);
            if (winwordKey != null)
            {
                string winwordPath = winwordKey.GetValue(string.Empty).ToString();
                if (!string.IsNullOrEmpty(winwordPath))
                {
                    return true;
                }
            }

            return false;
        }

        public static ConversionJob_Office.ApplicationName GetOfficeApplicationCompatibleWithExtension(string extension)
        {
            switch (extension)
            {
                case "doc":
                case "docx":
                case "odt":
                    return ConversionJob_Office.ApplicationName.Word;

                case "ppt":
                case "pptx":
                case "odp":
                    return ConversionJob_Office.ApplicationName.PowerPoint;

                case "ods":
                case "xls":
                case "xlsx":
                    return ConversionJob_Office.ApplicationName.Excel;
            }

            return ConversionJob_Office.ApplicationName.None;
        }

        public static class InputCategoryNames
        {
            public const string Audio = "Audio";
            public const string Video = "Video";
            public const string Image = "Image";
            public const string AnimatedImage = "Animated Image";
            public const string Document = "Document";

            public const string Misc = "Misc";
        }
    }
}