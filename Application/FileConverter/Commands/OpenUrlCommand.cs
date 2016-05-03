// <copyright file="OpenUrlCommand.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Commands
{
    using System;
    using System.Diagnostics;
    using System.Windows.Input;

    public class OpenUrlCommand : ICommand
    {
        public OpenUrlCommand()
        {
        }
        
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            string url = parameter as string;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            Process.Start(url);
        }
    }
}