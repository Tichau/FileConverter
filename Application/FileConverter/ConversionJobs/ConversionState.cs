// <copyright file="ConversionState.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    public enum ConversionState
    {
        Unknown,

        Ready,
        InProgress,
        Done,
        Failed,
    }
}
