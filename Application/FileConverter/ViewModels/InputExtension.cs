// <copyright file="InputExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Media;

    using CommonServiceLocator;
    using GalaSoft.MvvmLight;

    using FileConverter.Annotations;
    using FileConverter.ConversionJobs;

    public class InputExtension : ObservableObject
    {
        private readonly Brush defaultBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        private readonly Brush errorBrush = new SolidColorBrush(Color.FromRgb(255, 65, 0));

        private string name;
        private Brush foregroundBrush;
        private string toolTip;

        public InputExtension(string name)
        {
            this.Name = name;

            ConversionJob_Office.ApplicationName officeApplication = Helpers.GetOfficeApplicationCompatibleWithExtension(name);

            if (officeApplication == ConversionJob_Office.ApplicationName.None || Helpers.IsMicrosoftOfficeApplicationAvailable(officeApplication))
            {
                this.ForegroundBrush = this.defaultBrush;
            }
            else
            {
                this.ForegroundBrush = this.errorBrush;
                switch (officeApplication)
                {
                    case ConversionJob_Office.ApplicationName.Word:
                        this.ToolTip = Properties.Resources.ErrorMicrosoftWordIsNotAvailable;
                        break;

                    case ConversionJob_Office.ApplicationName.PowerPoint:
                        this.ToolTip = Properties.Resources.ErrorMicrosoftPowerPointIsNotAvailable;
                        break;

                    case ConversionJob_Office.ApplicationName.Excel:
                        this.ToolTip = Properties.Resources.ErrorMicrosoftExcelIsNotAvailable;
                        break;
                }
            }
        }

        public string Name
        {
            get => this.name;

            set
            {
                this.name = value;
                this.RaisePropertyChanged();
            }
        }

        public Brush ForegroundBrush
        {
            get => this.foregroundBrush;

            set
            {
                this.foregroundBrush = value;
                this.RaisePropertyChanged();
            }
        }

        public string ToolTip
        {
            get => this.toolTip;

            set
            {
                this.toolTip = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsChecked
        {
            get
            {
                SettingsViewModel settingsViewModel = ServiceLocator.Current.GetInstance<SettingsViewModel>();
                PresetNode selectedPreset = settingsViewModel.SelectedPreset;
                if (selectedPreset == null)
                {
                    return false;
                }

                return selectedPreset.Preset.InputTypes.Contains(this.name.ToLowerInvariant());
            }

            set
            {
                SettingsViewModel settingsViewModel = ServiceLocator.Current.GetInstance<SettingsViewModel>();
                PresetNode selectedPreset = settingsViewModel.SelectedPreset;

                if (value)
                {
                    selectedPreset?.Preset.AddInputType(this.name.ToLowerInvariant());
                }
                else
                {
                    selectedPreset?.Preset.RemoveInputType(this.name.ToLowerInvariant());
                }

                this.RaisePropertyChanged(nameof(this.IsChecked));
            }
        }
    }
}