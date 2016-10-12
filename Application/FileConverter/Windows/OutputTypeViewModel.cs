// <copyright file="OutputTypeViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Windows
{
    public class OutputTypeViewModel 
    {
        public OutputTypeViewModel(OutputType type)
        {
            this.Type = type;
            this.Category = Helpers.GetExtensionCategory(type.ToString().ToLowerInvariant());
        }

        public OutputType Type
        {
            get;
            private set;
        }

        public string Category
        {
            get;
            set;
        }

        public override bool Equals(object other)
        {
            OutputTypeViewModel outputTypeViewModel = other as OutputTypeViewModel;

            return outputTypeViewModel?.Type == this.Type;
        }

        public override int GetHashCode()
        {
            return this.Type.GetHashCode();
        }
    }
}