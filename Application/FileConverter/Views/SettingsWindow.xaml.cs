// <copyright file="SettingsWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Views
{
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    using FileConverter.ViewModels;

    using GalaSoft.MvvmLight.Messaging;

    /// <summary>
    /// Interaction logic for Settings.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            this.InitializeComponent();

            Messenger.Default.Register<string>(this, "DoFocus", this.DoFocus);
        }

        public void DoFocus(string message)
        {
            switch (message)
            {
                case "PresetName":
                    this.PresetNameTextBox.Focus();
                    this.PresetNameTextBox.SelectAll();
                    break;
            }
        }

        private void OnInputTypeChecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            if (!checkBox.IsVisible)
            {
                return;
            }

            string inputFormat = (checkBox.Content as string).ToLowerInvariant();

            dataContext.SelectedPreset.AddInputType(inputFormat);
        }

        private void OnInputTypeUnchecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            if (!checkBox.IsVisible)
            {
                return;
            }

            string inputFormat = (checkBox.Content as string).ToLowerInvariant();

            dataContext.SelectedPreset.RemoveInputType(inputFormat);
        }

        private void OnInputTypeCategoryChecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            string categoryName = checkBox.Content as string;
            InputExtensionCategory category = dataContext.InputCategories.FirstOrDefault(match => match.Name == categoryName);
            if (category == null)
            {
                return;
            }

            foreach (string inputExtension in category.InputExtensionNames)
            {
                dataContext.SelectedPreset.AddInputType(inputExtension);
            }
        }

        private void OnInputTypeCategoryUnchecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            string categoryName = checkBox.Content as string;
            InputExtensionCategory category = dataContext.InputCategories.FirstOrDefault(match => match.Name == categoryName);
            if (category == null)
            {
                return;
            }

            foreach (string inputExtension in category.InputExtensionNames)
            {
                dataContext.SelectedPreset.RemoveInputType(inputExtension);
            }
        }
        
        private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            SettingsViewModel settingsViewModel = (SettingsViewModel)this.DataContext;
            settingsViewModel.CloseCommand.Execute(null);
        }
    }
}
