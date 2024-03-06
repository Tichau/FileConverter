// <copyright file="ConversionJobToEstimatedRemainingDuration.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

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
            if (progress <= 0f)
            {
                return string.Empty;
            }

            if (state == ConversionState.Unknown ||
                state == ConversionState.Ready ||
                state == ConversionState.Failed ||
                state == ConversionState.Done)
            {
                return string.Empty;
            }

            TimeSpan elapsedTime = DateTime.Now - startTime;
            if (elapsedTime.TotalSeconds < 10f)
            {
                return string.Empty;
            }

            double remainingTimeInSeconds = (1 - progress) * elapsedTime.TotalSeconds / progress;
            TimeSpan remainingTime = TimeSpan.FromSeconds(Math.Floor(remainingTimeInSeconds));
            return "~" + remainingTime.ToString("g");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
