// <copyright file="Version.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.Xml.Serialization;

    public struct Version : System.IComparable<Version>
    {
        [XmlAttribute]
        public int Major;

        [XmlAttribute]
        public int Minor;

        [XmlAttribute]
        public int Patch;

        public static bool operator !=(Version a, Version b)
        {
            return a.CompareTo(b) != 0;
        }

        public static bool operator ==(Version a, Version b)
        {
            return a.CompareTo(b) == 0;
        }

        public static bool operator <=(Version a, Version b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >=(Version a, Version b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <(Version a, Version b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(Version a, Version b)
        {
            return a.CompareTo(b) > 0;
        }

        public override string ToString()
        {
            if (this.Patch == 0)
            {
                return $"{this.Major}.{this.Minor}";
            }

            return $"{this.Major}.{this.Minor}.{this.Patch}";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Version))
            {
                return false;
            }

            Version other = (Version)obj;

            return this.Major == other.Major && this.Minor == other.Minor && this.Patch == other.Patch;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int CompareTo(Version other)
        {
            if (this.Major == other.Major && this.Minor == other.Minor && this.Patch == other.Patch)
            {
                return 0;
            }

            if (this.Major > other.Major ||
                (this.Major == other.Major && this.Minor > other.Minor) ||
                (this.Major == other.Major && this.Minor == other.Minor && this.Patch > other.Patch))
            {
                return 1;
            }

            return -1;
        }
    }
}
