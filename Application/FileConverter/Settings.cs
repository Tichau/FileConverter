// <copyright file="Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Settings
    {
        public List<ConversionPreset> ConversionPresets = new List<ConversionPreset>();

        public void Load()
        {
            List<ConversionPreset> conversionPresets = this.ConversionPresets;
            conversionPresets.Clear();

            string userFilePath = this.GetSettingsUserFilePath();

            XmlHelpers.LoadFromFile<ConversionPreset>("Settings", userFilePath, ref conversionPresets);

            // TODO: If user settings doesn't exist, load the default settings.
        }

        public void Save()
        {
            string userFilePath = this.GetSettingsUserFilePath();
            XmlHelpers.SaveToFile("Settings", userFilePath, this.ConversionPresets);
        }

        private string GetSettingsUserFilePath()
        {
            string path = Environment.GetEnvironmentVariable("LocalAppData");
            path = Path.Combine(path, "FileConverter");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, "Settings.xml");
            return path;
        }
    }
}
