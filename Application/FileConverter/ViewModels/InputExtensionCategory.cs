// <copyright file="InputExtensionCategory.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using CommonServiceLocator;
    using GalaSoft.MvvmLight;
    using System.ComponentModel;
    using System.Collections.Generic;

    public class InputExtensionCategory : ObservableObject
    {
        private readonly List<InputExtension> inputExtensions = new List<InputExtension>();
        private string name;

        public InputExtensionCategory(string name)
        {
            this.Name = name;
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

        public IEnumerable<InputExtension> InputExtensions => this.inputExtensions;

        public IEnumerable<string> InputExtensionNames
        {
            get
            {
                foreach (InputExtension inputExtension in this.inputExtensions)
                {
                    yield return inputExtension.Name;
                }
            }
        }

        public void AddExtension(string extension)
        {
            InputExtension inputExtension = this.inputExtensions.Find(match => match.Name == extension);
            if (inputExtension == null)
            {
                inputExtension = new InputExtension(extension);
                this.inputExtensions.Add(inputExtension);

                inputExtension.PropertyChanged += this.OnExtensionPropertyChange;

                this.RaisePropertyChanged(nameof(this.InputExtensions));
                this.RaisePropertyChanged(nameof(this.InputExtensionNames));
                this.RaisePropertyChanged(nameof(this.IsChecked));
            }
        }

        public bool? IsChecked
        {
            get
            {
                SettingsViewModel settingsViewModel = ServiceLocator.Current.GetInstance<SettingsViewModel>();
                PresetNode selectedPreset = settingsViewModel.SelectedPreset;
                if (selectedPreset == null)
                {
                    return false;
                }

                bool all = true;
                bool none = true;
                foreach (string extension in this.InputExtensionNames)
                {
                    bool contains = selectedPreset.Preset.InputTypes.Contains(extension);
                    all &= contains;
                    none &= !contains;
                }

                if (all)
                {
                    return true;
                }
                else if (none)
                {
                    return false;
                }

                return null;
            }

            set
            {
                SettingsViewModel settingsViewModel = ServiceLocator.Current.GetInstance<SettingsViewModel>();
                PresetNode selectedPreset = settingsViewModel.SelectedPreset;
                
                foreach (string extension in this.InputExtensionNames)
                {
                    if (value == true)
                    {
                        selectedPreset?.Preset.AddInputType(extension);
                    }
                    else
                    {
                        selectedPreset?.Preset.RemoveInputType(extension);
                    }
                }

                // Raise property change for extensions.
                foreach (InputExtension inputExtension in this.InputExtensions)
                {
                    inputExtension.RaisePropertyChanged(nameof(inputExtension.IsChecked));
                }

                this.RaisePropertyChanged(nameof(this.IsChecked));
            }
        }

        private void OnExtensionPropertyChange(object sender, PropertyChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(this.IsChecked));
        }
    }
}