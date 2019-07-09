// <copyright file="InputExtensionCategory.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.Collections.Generic;
    using GalaSoft.MvvmLight;

    public class InputExtensionCategory : ObservableObject
    {
        private string name;
        private List<InputExtension> inputExtensions = new List<InputExtension>();

        public InputExtensionCategory(string name)
        {
            this.Name = name;
        }

        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
                this.RaisePropertyChanged();
            }
        }

        public IEnumerable<InputExtension> InputExtensions
        {
            get
            {
                return this.inputExtensions;
            }
        }

        public IEnumerable<string> InputExtensionNames
        {
            get
            {
                foreach (InputExtension inputExtension in this.inputExtensions)
                {
                    yield return inputExtension.Name;
                }
            }
        }

        public void AddExtension(string extension)
        {
            InputExtension inputExtension = this.inputExtensions.Find(match => match.Name == extension);
            if (inputExtension == null)
            {
                inputExtension = new InputExtension(extension);
                this.inputExtensions.Add(inputExtension);
                this.RaisePropertyChanged("InputExtensions");
                this.RaisePropertyChanged("InputExtensionNames");
            }
        }
    }
}