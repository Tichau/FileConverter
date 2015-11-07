// <copyright file="ConversionSettingsOverride.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.Collections;
    using System.Collections.Generic;

    public struct ConversionSettingsOverride : IConversionSettings
    {
        private string key;
        private string value;

        public ConversionSettingsOverride(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public int Count
        {
            get
            {
                return 1;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                yield return this.key;
            }
        }

        public IEnumerable<string> Values
        {
            get
            {
                yield return this.value;
            }
        }

        public string this[string key]
        {
            get
            {
                if (key == this.key)
                {
                    return this.value;
                }

                throw new KeyNotFoundException();
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield return new KeyValuePair<string, string>(this.key, this.value);
        }

        public bool ContainsKey(string key)
        {
            return key == this.key;
        }

        public bool TryGetValue(string key, out string value)
        {
            value = null;

            if (key == this.key)
            {
                value = this.value;
                return true;
            }

            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
