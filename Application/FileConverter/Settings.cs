// <copyright file="Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;

    public class Settings
    {
        public List<ConversionPreset> conversionPresets = new List<ConversionPreset>();

        public void Load()
        {
            // TODO: Load from file.

            this.conversionPresets.Add(new ConversionPreset("To Flac", OutputType.Flac, new string[] { "Mp3", "Wav", "Ogg", "Wma" }));
            this.conversionPresets.Add(new ConversionPreset("To Ogg", OutputType.Ogg, new string[] { "Mp3", "Wav", "Flac", "Wma" }));
        }
    }
}
