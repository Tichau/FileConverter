// <copyright file="DependencyStatusViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;

    public class DependencyStatusViewModel : ObservableObject
    {
        private string name;
        private string status;
        private string details;
        private bool isHealthy;

        public DependencyStatusViewModel(string name, string status, string details, bool isHealthy)
        {
            this.Name = name;
            this.Status = status;
            this.Details = details;
            this.IsHealthy = isHealthy;
        }

        public string Name
        {
            get => this.name;
            private set => this.SetProperty(ref this.name, value);
        }

        public string Status
        {
            get => this.status;
            private set => this.SetProperty(ref this.status, value);
        }

        public string Details
        {
            get => this.details;
            private set => this.SetProperty(ref this.details, value);
        }

        public bool IsHealthy
        {
            get => this.isHealthy;
            private set => this.SetProperty(ref this.isHealthy, value);
        }
    }
}
