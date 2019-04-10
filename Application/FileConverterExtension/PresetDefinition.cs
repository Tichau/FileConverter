// <copyright file="PresetDefinition.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    public class PresetDefinition
    {
        public PresetDefinition(string name)
        {
            this.Name = name;
        }

        public string Name
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
