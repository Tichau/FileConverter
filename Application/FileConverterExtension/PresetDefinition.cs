// <copyright file="PresetDefinition.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    public class PresetDefinition
    {
        public PresetDefinition(string fullName, string name, string[] folders)
        {
            this.FullName = fullName;
            this.Name = name;
            this.Folders = folders;
        }

        public string FullName
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string[] Folders
        {
            get;
            private set;
        }

        public bool Enabled
        {
            get;
            set;
        }

        public int ExtensionRefCount
        {
            get;
            set;
        }
    }
}
