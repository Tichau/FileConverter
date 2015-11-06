// <copyright file="IXmlSerializable.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    public interface IXmlSerializable
    {
        void OnDeserializationComplete();
    }
}