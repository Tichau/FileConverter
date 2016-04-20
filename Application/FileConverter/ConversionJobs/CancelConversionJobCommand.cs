// <copyright file="CancelConversionJobCommand.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.Windows.Input;

    public class CancelConversionJobCommand : ICommand
    {
        private readonly ConversionJob conversionJob;

        public CancelConversionJobCommand(ConversionJob conversionJob)
        {
            this.conversionJob = conversionJob;
            this.conversionJob.PropertyChanged += this.ConversionJob_PropertyChanged;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (this.conversionJob == null)
            {
                return false;
            }

            return this.conversionJob.IsCancelable && this.conversionJob.State == ConversionJob.ConversionState.InProgress;
        }

        public void Execute(object parameter)
        {
            this.conversionJob?.Cancel();
        }

        private void ConversionJob_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName != "State")
            {
                return;
            }

            this.CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }
}