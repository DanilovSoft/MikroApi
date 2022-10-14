using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DanilovSoft.MikroApi;

/// <summary>
/// Представляет коллекцию ключей и значений.
/// </summary>
[DebuggerDisplay("{DebugDisplay,nq}")]
[DebuggerTypeProxy(typeof(TypeProxy))]
public sealed class MikroTikResponseFrameDictionary : IReadOnlyDictionary<string, string>
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
    public T? Value<T>(string key)
    {
        return (T?)MikroTikTypeConverter.ConvertValue(_dict[key], typeof(T));
    }

    public string? this[string key, bool nullIfNotExist]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var n = 0;
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

    internal void TryAdd(string key, string value)
    {
        _dict.TryAdd(key, value);
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
    private sealed class TypeProxy
    {
        private readonly MikroTikResponseFrameDictionary _self;
        public TypeProxy(MikroTikResponseFrameDictionary self)
        {
            _self = self;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        [SuppressMessage("CodeQuality", "IDE0051:Удалите неиспользуемые закрытые члены", Justification = "Debug Display")]
        private KeyValuePair<string, string>[] Items => _self._dict.ToArray();
    }
}
