// <copyright file="Diagnostics.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Windows;

    public static class Debug
    {
        private static StringBuilder workingStringBuilder = new StringBuilder();
        private static string diagnosticsFolderPath;
        private static Dictionary<int, DiagnosticsData> diagnosticsDataById = new Dictionary<int, DiagnosticsData>();
        private static int threadCount = 0;

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        static Debug()
        {
            string path = PathHelpers.GetUserDataFolderPath();

            // Delete old diagnostics folder.
            DateTime expirationDate = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
            string[] diagnosticsDirectories = Directory.GetDirectories(path, "Diagnostics.*");
            for (int index = 0; index < diagnosticsDirectories.Length; index++)
            {
                string directory = diagnosticsDirectories[index];
                DateTime creationTime = Directory.GetCreationTime(directory);
                if (creationTime < expirationDate)
                {
                    Directory.Delete(directory, true);
                }
            }

            Debug.diagnosticsFolderPath = Path.Combine(path, "Diagnostics." + Process.GetCurrentProcess().Id);
            Debug.diagnosticsFolderPath = PathHelpers.GenerateUniquePath(Debug.diagnosticsFolderPath);
            Directory.CreateDirectory(Debug.diagnosticsFolderPath);
        }

        public static IEnumerable<DiagnosticsData> Data
        {
            get
            {
                return Debug.diagnosticsDataById.Values;
            }
        }

        public static string Content
        {
            get
            {
                DiagnosticsData diagnosticsData;
                int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if (!Debug.diagnosticsDataById.TryGetValue(threadId, out diagnosticsData))
                {
                    return string.Empty;
                }

                workingStringBuilder.Clear();
                foreach (string logMessage in diagnosticsData.LogMessages)
                {
                    workingStringBuilder.AppendLine(logMessage);
                }

                return workingStringBuilder.ToString();
            }
        }

        public static void Log(string message, params object[] arguments)
        {
            DiagnosticsData diagnosticsData;

            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            lock (Debug.diagnosticsDataById)
            {
                if (!Debug.diagnosticsDataById.TryGetValue(threadId, out diagnosticsData))
                {
                    diagnosticsData = new DiagnosticsData(Debug.threadCount > 0 ? string.Format("Thread {0}", Debug.threadCount) : "Application");
                    diagnosticsData.Initialize(Debug.diagnosticsFolderPath, threadId);
                    Debug.diagnosticsDataById.Add(threadId, diagnosticsData);
                    Debug.threadCount++;
                }
            }

            diagnosticsData.Log(message, arguments);
            
            if (StaticPropertyChanged != null)
            {
                StaticPropertyChanged(null, new PropertyChangedEventArgs("Content"));
            }
        }

        public static void LogError(string message, params object[] arguments)
        {
            string log = string.Format(message, arguments);

            MessageBox.Show(log, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            Debug.Log("Error: " + message, arguments);
        }

        public static void Release()
        {
            Debug.Log("Diagnostics manager released correctly.");

            foreach (KeyValuePair<int, DiagnosticsData> kvp in Debug.diagnosticsDataById)
            {
                kvp.Value.Release();
            }

            Debug.diagnosticsDataById.Clear();
        }
        
    }
}
