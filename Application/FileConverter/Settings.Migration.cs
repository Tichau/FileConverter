// // <copyright file="Settings.Migration.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;

    using FileConverter.ConversionJobs;

    public partial class Settings
    {
        private static void MigrateSettingsToCurrentVersion(Settings settings)
        {
            int settingsVersion = settings.SerializationVersion;

            // Migrate conversion settings.
            if (settings.ConversionPresets != null)
            {
                foreach (ConversionPreset conversionPreset in settings.ConversionPresets)
                {
                    Settings.MigrateConversionPresetToCurrentVersion(conversionPreset, settingsVersion);
                }
            }
        }

        private static void MigrateConversionPresetToCurrentVersion(ConversionPreset preset, int settingsVersion)
        {
            if (settingsVersion <= 2)
            {
                // Migrate video encoding speed.
                string videoEncodingSpeed = preset.GetSettingsValue(ConversionPreset.ConversionSettingKeys.VideoEncodingSpeed);
                if (videoEncodingSpeed != null)
                {
                    VideoEncodingSpeed encodingSpeed;
                    if (!Enum.TryParse(videoEncodingSpeed, out encodingSpeed))
                    {
                        switch (videoEncodingSpeed)
                        {
                            case "Ultra Fast":
                                preset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.VideoEncodingSpeed, VideoEncodingSpeed.UltraFast.ToString());
                                break;

                            case "Super Fast":
                                preset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.VideoEncodingSpeed, VideoEncodingSpeed.SuperFast.ToString());
                                break;

                            case "Very Fast":
                                preset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.VideoEncodingSpeed, VideoEncodingSpeed.VeryFast.ToString());
                                break;

                            case "Very Slow":
                                preset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.VideoEncodingSpeed, VideoEncodingSpeed.VerySlow.ToString());
                                break;
                        }
                    }
                }
            }

            if (settingsVersion <= 3)
            {
                // Try to fix corrupted settings (GitHub issue #5).
                string scale = preset.GetSettingsValue(ConversionPreset.ConversionSettingKeys.ImageScale);
                if (scale != null)
                {
                    preset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.ImageScale, scale.Replace(',', '.'));
                }

                scale = preset.GetSettingsValue(ConversionPreset.ConversionSettingKeys.VideoScale);
                if (scale != null)
                {
                    preset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.VideoScale, scale.Replace(',', '.'));
                }
            }
        }
    }
}