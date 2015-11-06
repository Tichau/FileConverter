// <copyright file="InputExtensionCategory.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FileConverter.Annotations;

namespace FileConverter
{
    public class InputExtensionCategory : INotifyPropertyChanged
    {
        private string name;
        private List<InputExtension> inputExtensions = new List<InputExtension>();

        public event PropertyChangedEventHandler PropertyChanged;

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
                this.OnPropertyChanged();
            }
        }

        public IEnumerable<InputExtension> InputExtensions
        {
            get
            {
                return this.inputExtensions;
            }
        } 

        public void AddExtension(string extension)
        {
            InputExtension inputExtension = this.inputExtensions.Find(match => match.Name == extension);
            if (inputExtension == null)
            {
                inputExtension = new InputExtension(extension);
                this.inputExtensions.Add(inputExtension);
                this.OnPropertyChanged("InputExtensions");
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}