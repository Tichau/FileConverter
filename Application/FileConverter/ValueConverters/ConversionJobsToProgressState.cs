// <copyright file="ConversionJobsToProgressState.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Shell;

    using FileConverter.ConversionJobs;

    public class ConversionJobsToProgressState : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IEnumerable<ConversionJob>))
            {
                return TaskbarItemProgressState.None;
            }

            IEnumerable<ConversionJob> jobs = (IEnumerable<ConversionJob>)value;

            TaskbarItemProgressState progressState = TaskbarItemProgressState.None;
            foreach (ConversionJob job in jobs)
            {
                if (job.State == ConversionJob.ConversionState.Failed)
                {
                    progressState = TaskbarItemProgressState.Error;
                    break;
                }

                if (job.State == ConversionJob.ConversionState.InProgress)
                {
                    progressState = TaskbarItemProgressState.Normal;
                    break;
                }
            }

            return progressState;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
