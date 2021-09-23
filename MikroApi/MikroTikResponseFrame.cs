using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace DanilovSoft.MikroApi
{
    /// <summary>
    /// Представляет коллекцию ключей и значений.
    /// </summary>
    [DebuggerDisplay("{DebugDisplay,nq}")]
    [DebuggerTypeProxy(typeof(TypeProxy))]
    public sealed class MikroTikResponseFrame : IReadOnlyDictionary<string, string>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"Count = {_dict.Count}";
        private readonly Dictionary<string, string> _dict = new();

        public IEnumerable<string> Keys => _dict.Keys;

        public IEnumerable<string> Values => _dict.Values;

        public int Count => _dict.Count;

        public string this[string key] => _dict[key];

        /// <summary>
        /// Преобразует строковое значение в требуемый тип.
        /// </summary>
        public T Value<T>(string key)
        {
            return (T)MikroTikTypeConverter.ConvertValue(_dict[key], typeof(T));
        }

        public string? this[string key, bool nullIfNotExist]
        {
            get
            {
                if (TryGetValue(key, out string value))
                {
                    return value;
                }

                return null;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            int n = 0;
            foreach (var item in this)
            {
                if (n > 0)
                {
                    sb.AppendLine();
                }

                n++;
                sb.AppendFormat(CultureInfo.InvariantCulture, "\"{0}\"=\"{1}\"", item.Key, item.Value);
            }
            return sb.ToString();
        }

        internal void Add(string key, string value)
        {
            _dict.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [DebuggerNonUserCode]
        private class TypeProxy
        {
            private readonly MikroTikResponseFrame _self;
            public TypeProxy(MikroTikResponseFrame self)
            {
                _self = self;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            private KeyValuePair[] Items
            {
                get
                {
                    KeyValuePair[] items = new KeyValuePair[_self._dict.Count];
                    int i = 0;
                    foreach (var item in _self._dict)
                    {
                        items[i] = new KeyValuePair(item.Key, item.Value);
                        i++;
                    }
                    return items;
                }
            }
        }

        [DebuggerNonUserCode]
        [DebuggerDisplay("\\{[{Key,nq}, {Value,nq}]\\}")]
        private struct KeyValuePair
        {
            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; }
            public string Value { get; }
        }
    }
}
