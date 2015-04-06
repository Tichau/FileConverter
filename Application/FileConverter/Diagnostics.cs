// <copyright file="Diagnostics.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;

    public static class Diagnostics
    {
        private static List<string> logMessages = new List<string>();
        private static StringBuilder workingStringBuilder = new StringBuilder();

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        public static string Content
        {
            get
            {
                workingStringBuilder.Clear();
                for (int index = 0; index < logMessages.Count; index++)
                {
                    workingStringBuilder.AppendLine(logMessages[index]);
                }

                return workingStringBuilder.ToString();
            }
        }

        public static void Log(string message, params object[] arguments)
        {
            Diagnostics.logMessages.Add(string.Format(message, arguments));

            if (StaticPropertyChanged != null)
            {
                StaticPropertyChanged(null, new PropertyChangedEventArgs("Content"));
            }
        }
    }
}
