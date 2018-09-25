// <copyright file="ConversionJobsToProgressValue.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using ConversionJobs;

    public class ConversionJobToEstimatedRemainingDuration : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3)
            {
                return string.Empty;
            }

            ConversionState state = (ConversionState)values[0];
            DateTime startTime = (DateTime)values[1];
            float progress = (float)values[2];

            if (state == ConversionState.Unknown ||
                state == ConversionState.Ready ||
                progress < 0.05f)
            {
                return "-";
            }

            if (progress >= 1f)
            {
                return "-";
            }

            TimeSpan elapsedTime = DateTime.Now - startTime;

            double remainingTimeInSeconds = (1 - progress) * elapsedTime.TotalSeconds / progress;
            TimeSpan remainingTime = TimeSpan.FromSeconds(Math.Floor(remainingTimeInSeconds));
            return remainingTime.ToString("g");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
