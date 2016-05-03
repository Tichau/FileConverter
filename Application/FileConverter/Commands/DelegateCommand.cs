// <copyright file="DelegateCommand.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Commands
{
    using System;
    using System.Windows.Input;

    public class DelegateCommand : ICommand
    {
        private Action executeMethod;
        private Func<bool> canExecuteMethod;

        public DelegateCommand(Action executeMethod)
        {
            if (executeMethod == null)
            {
                throw new ArgumentNullException(nameof(executeMethod));
            }

            this.executeMethod = executeMethod;
        }

        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (this.canExecuteMethod != null)
            {
                return this.canExecuteMethod.Invoke();
            }

            return true;
        }

        public void Execute(object parameter)
        {
            this.executeMethod.Invoke();
        }
    }
}