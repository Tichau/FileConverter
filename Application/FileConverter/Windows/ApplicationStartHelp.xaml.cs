// <copyright file="ApplicationStartHelp.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Windows
{
    using System;
    using System.Windows;
    
    public partial class ApplicationStartHelp : Window
    {
        public ApplicationStartHelp()
        {
            this.InitializeComponent();
        }

        public static FileConverter.Version ApplicationVersion
        {
            get
            {
                return FileConverter.Application.ApplicationVersion;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
        }
    }
}
