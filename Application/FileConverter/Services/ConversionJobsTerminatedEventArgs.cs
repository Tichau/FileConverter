// <copyright file="ConversionJobsTerminatedEventArgs.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;

    public class ConversionJobsTerminatedEventArgs : EventArgs
    {
        public ConversionJobsTerminatedEventArgs(bool allConversionsSucceed)
        {
            this.AllConversionsSucceed = allConversionsSucceed;
        }

        public bool AllConversionsSucceed
        {
            get;
            private set;
        }
    }
}
