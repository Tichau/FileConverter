// <copyright file="ConversionJobsToProgressValue.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Shell;

    using FileConverter.ConversionJobs;

    public class ConversionJobsToProgressValue : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IEnumerable<ConversionJob>))
            {
                return TaskbarItemProgressState.None;
            }

            IEnumerable<ConversionJob> jobs = (IEnumerable<ConversionJob>)value;

            int jobCount = 0;
            float progressValue = 0f;
            foreach (ConversionJob job in jobs)
            {
                progressValue += job.Progress;
                jobCount++;
            }

            return jobCount > 0 ? progressValue / jobCount : 0f;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
