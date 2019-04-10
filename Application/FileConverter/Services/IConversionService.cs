// <copyright file="IConversionService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System.Collections.ObjectModel;

    using FileConverter.ConversionJobs;

    public interface IConversionService
    {
        event System.EventHandler<ConversionJobsTerminatedEventArgs> ConversionJobsTerminated;

        ReadOnlyCollection<ConversionJob> ConversionJobs
        {
            get;
        }

        void ConvertFilesAsync();

        void RegisterConversionJob(ConversionJob conversionJob);
    }
}
