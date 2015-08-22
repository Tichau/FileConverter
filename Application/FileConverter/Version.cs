// <copyright file="Version.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    public struct Version
    {
        public int Major;
        public int Minor;

        public override string ToString()
        {
            return string.Format("{0}.{1}", this.Major, this.Minor);
        }
    }
}
