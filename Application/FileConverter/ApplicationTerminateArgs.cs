// <copyright file="ApplicationTerminateArgs.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    public class ApplicationTerminateArgs : System.EventArgs
    {
        public ApplicationTerminateArgs(float remainingTimeBeforeTermination)
        {
            this.RemainingTimeBeforeTermination = remainingTimeBeforeTermination;
        }

        public float RemainingTimeBeforeTermination
        {
            get;
            private set;
        }
    }
}