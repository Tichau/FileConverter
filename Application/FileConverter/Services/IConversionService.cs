// <copyright file="IConversionService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System.Collections.ObjectModel;

    using FileConverter.ConversionJobs;

    public interface IConversionService
    {
        ReadOnlyCollection<ConversionJob> ConversionJobs
        {
            get;
        }

        event System.EventHandler<ConversionJobsTerminatedEventArgs> ConversionJobsTerminated;

        void ConvertFilesAsync();

        void RegisterConversionJob(ConversionJob conversionJob);
    }
}
