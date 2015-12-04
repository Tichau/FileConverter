// <copyright file="DiagnosticsData.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System.Collections.ObjectModel;

namespace FileConverter.Diagnostics
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Runtime.CompilerServices;

    using FileConverter.Annotations;

    public class DiagnosticsData : INotifyPropertyChanged
    {
        private List<string> logMessages = new List<string>();
        private StringBuilder stringBuilder = new StringBuilder();
        private System.IO.TextWriter logFileWriter;
        private string name;

        public event PropertyChangedEventHandler PropertyChanged;

        public DiagnosticsData(string name)
        {
            this.Name = name;
            this.LogMessages = new ReadOnlyCollection<string>(this.logMessages);
        }

        public string Name
        {
            get
            {
                return this.name;
            }

            private set
            {
                this.name = value;
                this.OnPropertyChanged();
            }
        }

        public string Content
        {
            get
            {
                this.stringBuilder.Clear();
                foreach (string logMessage in this.LogMessages)
                {
                    this.stringBuilder.AppendLine(logMessage);
                }

                return this.stringBuilder.ToString();
            }
        }

        public ReadOnlyCollection<string> LogMessages
        {
            get;
            private set;
        }

        public void Initialize(string diagnosticsFolderPath, int id)
        {
            string path = Path.Combine(diagnosticsFolderPath, string.Format("Diagnostics{0}.log", id));
            path = PathHelpers.GenerateUniquePath(path);
            this.logFileWriter = new StreamWriter(File.Open(path, FileMode.Create));

            this.Log("{0} {1}\n", System.DateTime.Now.ToLongDateString(), System.DateTime.Now.ToLongTimeString());
        }

        public void Release()
        {
            this.logFileWriter.Close();
            this.logFileWriter = null;
        }

        public void Log(string message, params object[] arguments)
        {
            string log = string.Format(message, arguments);

            this.logMessages.Add(log);
            this.logFileWriter.WriteLine(log);
            this.logFileWriter.Flush();

            this.OnPropertyChanged("Content");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}