// <copyright file="ConversionJobRegisteredEventArgs.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;

    using FileConverter.ConversionJobs;

    public class ConversionJobRegisteredEventArgs : EventArgs
    {
        public ConversionJobRegisteredEventArgs(ConversionJob conversionJob)
        {
            this.ConversionJob = conversionJob;
        }

        public ConversionJob ConversionJob
        {
            get;
        }
    }
}
