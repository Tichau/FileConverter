using System.Collections.Generic;

namespace FileConverter
{
    public interface IConversionSettings : IReadOnlyDictionary<string, string>
    {
    }

    public class ConversionSettings : Dictionary<string, string>, IConversionSettings
    {
    }
}