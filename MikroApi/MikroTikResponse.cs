using DanilovSoft.MikroApi.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace DanilovSoft.MikroApi;

/// <summary>
/// Результат выполнение команды микротиком.
/// </summary>
[DebuggerDisplay("{DebugDisplay,nq}")]
[DebuggerTypeProxy(typeof(TypeProxy))]
public sealed class MikroTikResponse : IReadOnlyList<MikroTikResponseFrameDictionary>
{
    private const string MoreThanOneColumn = "There is more than one column.";
    private const string MoreThanOneRow = "There is more than one row.";
    private const string CollectionIsEmpty = "Collection is empty.";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugDisplay => $"Count = {_receivedFrames.Count}";
    private readonly List<MikroTikResponseFrameDictionary> _receivedFrames = new();

    public MikroTikResponseFrameDictionary this[int index] 
    { 
        get => _receivedFrames[index]; 
        set => _receivedFrames[index] = value; 
    }

    public int Count => _receivedFrames.Count;

    internal void Add(MikroTikResponseFrameDictionary frame)
    {
        _receivedFrames.Add(frame);
    }

    #region Scalar

    /// <exception cref="InvalidOperationException"/>
    public string Scalar()
    {
        // Должна быть только одна строка.
        if (Count == 1)
        {
            var dict = this[0];

            // Должна быть только одна колонка.
            if (dict.Count == 1)
            {
                using (var enumerator = dict.GetEnumerator())
                {
                    enumerator.MoveNext();
                    return enumerator.Current.Value;
                }
            }
            else
            {
                if (dict.Count > 1)
                {
                    throw new InvalidOperationException(MoreThanOneColumn);
                }
                else
                {
                    // Пустого словаря вероятно не может быть. МТ пропускает пустые словари.
                    throw new InvalidOperationException(CollectionIsEmpty);
                }
            }
        }
        else
        {
            if (Count == 0)
            {
                throw new InvalidOperationException(CollectionIsEmpty);
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    /// <exception cref="InvalidOperationException"/>
    public T? Scalar<T>()
    {
        var rawValue = Scalar();
        return MikroTikTypeConverter.ConvertValue<T>(rawValue);
    }

    /// <exception cref="InvalidOperationException"/>
    public string Scalar(string columnName)
    {
        // Должна быть только одна строка.
        if (Count == 1)
        {
            return this[0][columnName];
        }
        else
        {
            if (Count == 0)
            {
                throw new InvalidOperationException(CollectionIsEmpty);
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    /// <exception cref="InvalidOperationException"/>
    public T? Scalar<T>(string columnName)
    {
        var rawValue = Scalar(columnName);
        return MikroTikTypeConverter.ConvertValue<T>(rawValue);
    }

    #endregion

    #region ScalarArray

    public string[] ScalarArray(string columnName)
    {
        // Если есть строки.
        if (Count > 0)
        {
            var array = new string[Count];
            for (var i = 0; i < Count; i++)
            {
                array[i] = this[i][columnName];
            }
            return array;
        }
        else
        {
            return Array.Empty<string>();
        }
    }

    public T?[] ScalarArray<T>(string columnName)
    {
        // Если есть строки.
        if (Count > 0)
        {
            var array = new T?[Count];
            for (var i = 0; i < Count; i++)
            {
                var rawValue = this[i][columnName];
                array[i] = MikroTikTypeConverter.ConvertValue<T>(rawValue);
            }
            return array;
        }
        else
        {
            return Array.Empty<T>();
        }
    }

    /// <exception cref="InvalidOperationException"/>
    public string[] ScalarArray()
    {
        // Если есть строки.
        if (Count > 0)
        {
            var dict = this[0];

            // Должна быть только одна колонка.
            if (dict.Count == 1)
            {
                string key;
                using (var enumerator = dict.GetEnumerator())
                {
                    enumerator.MoveNext();
                    key = enumerator.Current.Key;
                }

                var array = new string[Count];
                for (var i = 0; i < Count; i++)
                {
                    array[i] = this[i][key];
                }
                return array;
            }
            else
            {
                if (dict.Count > 0)
                {
                    throw new InvalidOperationException(MoreThanOneColumn);
                }
                else
                {
                    throw new InvalidOperationException(CollectionIsEmpty);
                }
            }
        }
        else
        {
            return Array.Empty<string>();
        }
    }

    /// <exception cref="InvalidOperationException"/>
    public T?[] ScalarArray<T>()
    {
        // Если есть строки.
        if (Count > 0)
        {
            var dict = this[0];

            // Должна быть только одна колонка.
            if (dict.Count == 1)
            {
                string key;
                using (var enumerator = dict.GetEnumerator())
                {
                    enumerator.MoveNext();
                    key = enumerator.Current.Key;
                }

                var array = new T?[Count];
                for (var i = 0; i < Count; i++)
                {
                    array[i] = MikroTikTypeConverter.ConvertValue<T>(this[i][key]);
                }
                return array;
            }
            else
            {
                if (dict.Count > 0)
                {
                    throw new InvalidOperationException(MoreThanOneColumn);
                }
                else
                {
                    throw new InvalidOperationException(CollectionIsEmpty);
                }
            }
        }
        else
        {
            return Array.Empty<T>();
        }
    }

    #endregion

    #region ScalarList

    /// <exception cref="InvalidOperationException"/>
    public List<string> ScalarList()
    {
        // Если есть строки.
        if (Count > 0)
        {
            var dict = this[0];

            // Должна быть только одна колонка.
            if (dict.Count == 1)
            {
                string key;
                using (var enumerator = dict.GetEnumerator())
                {
                    enumerator.MoveNext();
                    key = enumerator.Current.Key;
                }

                var list = new List<string>(Count);
                for (var i = 0; i < Count; i++)
                {
                    list.Add(this[i][key]);
                }
                return list;
            }
            else
            {
                if (dict.Count > 0)
                {
                    throw new InvalidOperationException(MoreThanOneColumn);
                }
                else
                {
                    throw new InvalidOperationException(CollectionIsEmpty);
                }
            }
        }
        else
        {
            return new List<string>();
        }
    }

    /// <exception cref="InvalidOperationException"/>
    public List<T?> ScalarList<T>()
    {
        // Если есть строки.
        if (Count > 0)
        {
            var dict = this[0];

            // Должна быть только одна колонка.
            if (dict.Count == 1)
            {
                string key;
                using (var enumerator = dict.GetEnumerator())
                {
                    enumerator.MoveNext();
                    key = enumerator.Current.Key;
                }

                var list = new List<T?>(Count);
                for (var i = 0; i < Count; i++)
                {
                    var rawValue = this[i][key];
                    list.Add(MikroTikTypeConverter.ConvertValue<T>(rawValue));
                }
                return list;
            }
            else
            {
                if (dict.Count > 0)
                {
                    throw new InvalidOperationException(MoreThanOneColumn);
                }
                else
                {
                    throw new InvalidOperationException(CollectionIsEmpty);
                }
            }
        }
        else
        {
            return new List<T?>();
        }
    }

    public List<string> ScalarList(string columnName)
    {
        // Если есть строки.
        if (Count > 0)
        {
            var list = new List<string>(Count);
            for (var i = 0; i < Count; i++)
            {
                list.Add(this[i][columnName]);
            }
            return list;
        }
        else
        {
            return new List<string>();
        }
    }

    public List<T?> ScalarList<T>(string columnName)
    {
        // Если есть строки.
        if (Count > 0)
        {
            var list = new List<T?>(Count);
            for (var i = 0; i < Count; i++)
            {
                var rawValue = this[i][columnName];
                list.Add(MikroTikTypeConverter.ConvertValue<T>(rawValue));
            }
            return list;
        }
        else
        {
            return new List<T?>();
        }
    }

    #endregion

    #region ScalarOrDefault

    /// <summary>
    /// Возвращает <see langword="null"/> если нет ни одной строки.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    public string? ScalarOrDefault(string columnName)
    {
        // Должна быть только одна строка.
        if (Count == 1)
        {
            return this[0][columnName];
        }
        else
        {
            if (Count == 0)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    /// <summary>
    /// Возвращает <see langword="default"/> если нет ни одной строки.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public T? ScalarOrDefault<T>(string columnName)
    {
        var rawValue = ScalarOrDefault(columnName);

        if (rawValue != null)
        {
            return MikroTikTypeConverter.ConvertValue<T>(rawValue);
        }
        else
        {
            return default;
        }
    }

    /// <summary>
    /// Возвращает <see langword="null"/> если нет ни одной строки.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    public string? ScalarOrDefault()
    {
        // Должна быть только одна строка.
        if (Count == 1)
        {
            var dict = this[0];

            // Должна быть только одна колонка.
            if (dict.Count == 1)
            {
                using (var enumerator = dict.GetEnumerator())
                {
                    enumerator.MoveNext();
                    return enumerator.Current.Value;
                }
            }
            else
            {
                if (dict.Count > 1)
                {
                    throw new InvalidOperationException(MoreThanOneColumn);
                }
                else
                {
                    // Пустого словаря вероятно не может быть. МТ пропускает пустые словари.
                    throw new InvalidOperationException(CollectionIsEmpty);
                }
            }
        }
        else
        {
            if (Count == 0)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    /// <summary>
    /// Возвращает <see langword="default"/> если нет ни одной строки.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    public T? ScalarOrDefault<T>()
    {
        var rawValue = ScalarOrDefault();

        if (rawValue != null)
        {
            return MikroTikTypeConverter.ConvertValue<T>(rawValue);
        }

        return default;
    }

    #endregion

    #region Single

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public MikroTikResponseFrameDictionary Single()
    {
        // Должна быть только одна строка.
        if (Count == 1)
        {
            return this[0];
        }
        else
        {
            if (Count == 0)
            {
                throw new InvalidOperationException(CollectionIsEmpty);
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public T Single<T>(Func<MikroTikResponseFrameDictionary, T> selector)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }
        // Должна быть только одна строка.
        if (Count == 1)
        {
            var frame = this[0];
            return selector(frame);
        }
        else
        {
            if (Count == 0)
            {
                throw new InvalidOperationException(CollectionIsEmpty);
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public T Single<T>()
    {
        // Должна быть только одна строка.
        if (Count == 1)
        {
            var dict = this[0];
            var mapper = DynamicActivator.GetMapper<T>();
            return (T)mapper.ReadObject(dict);
        }
        else
        {
            if (Count == 0)
            {
                throw new InvalidOperationException(CollectionIsEmpty);
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    #endregion

    #region SingleOrDefault

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public MikroTikResponseFrameDictionary? SingleOrDefault()
    {
        // Должна быть только одна строка.
        if (Count == 1)
        {
            var dict = this[0];
            return dict;
        }
        else
        {
            if (Count == 0)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public T? SingleOrDefault<T>(Func<MikroTikResponseFrameDictionary, T> selector)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }
        // Должна быть только одна строка.
        if (Count == 1)
        {
            var frame = this[0];
            return selector(frame);
        }
        else
        {
            if (Count == 0)
            {
                return default;
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public T? SingleOrDefault<T>()
    {
        // Должна быть только одна строка.
        if (Count == 1)
        {
            var dict = this[0];
            var mapper = DynamicActivator.GetMapper<T>();
            return (T)mapper.ReadObject(dict);
        }
        else
        {
            if (Count == 0)
            {
                return default;
            }
            else
            {
                throw new InvalidOperationException(MoreThanOneRow);
            }
        }
    }

    #endregion

    #region ToArray

    /// <summary>
    /// Создает список объектов
    /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T[] ToArray<T>()
    {
        if (Count > 0)
        {
            var array = new T[Count];
            var mapper = DynamicActivator.GetMapper<T>();
            for (var i = 0; i < Count; i++)
            {
                array[i] = (T)mapper.ReadObject(this[i]);
            }
            return array;
        }
        return Array.Empty<T>();
    }

    public T[] ToArray<T>(Func<MikroTikResponseFrameDictionary, T> selector)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        if (Count > 0)
        {
            var array = new T[Count];
            for (var i = 0; i < Count; i++)
            {
                array[i] = selector(this[i]);
            }
            return array;
        }
        return Array.Empty<T>();
    }


    /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
    public T[] ToArray<T>(T anonymousObject)
    {
        if (Count > 0)
        {
            var mapper = DynamicActivator.GetAnonymousMappger<T>();
            var array = new T[Count];
            for (var i = 0; i < Count; i++)
            {
                array[i] = (T)mapper.ReadObject(this[i]);
            }
            return array;
        }
        return Array.Empty<T>();
    }

    #endregion

    #region ToList

    /// <summary>
    /// Создает список объектов
    /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public List<T> ToList<T>()
    {
        var list = new List<T>(Count);
        if (Count > 0)
        {
            var mapper = DynamicActivator.GetMapper<T>();
            for (var i = 0; i < Count; i++)
            {
                list.Add((T)mapper.ReadObject(this[i]));
            }
        }

        return list;
    }

    public List<T> ToList<T>(Func<MikroTikResponseFrameDictionary, T> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        
        var list = new List<T>(Count);
        for (var i = 0; i < Count; i++)
        {
            var selected = selector(this[i]);
            list.Add(selected);
        }
        return list;
    }

    /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
    public List<T> ToList<T>(T anonymousObject)
    {
        var list = new List<T>(Count);
        if (Count > 0)
        {
            var mapper = DynamicActivator.GetAnonymousMappger<T>();
            for (var i = 0; i < Count; i++)
            {
                list.Add((T)mapper.ReadObject(this[i]));
            }
        }
        return list;
    }

    #endregion

    public override string ToString()
    {
        if (Count == 1)
        {
            return this[0].ToString();
        }

        var sb = new StringBuilder();
        var n = 0;
        foreach (var frame in CollectionsMarshal.AsSpan(_receivedFrames))
        {
            if (n > 0)
            {
                sb.AppendLine();
            }

            n++;
            sb.Append(frame.ToString());
        }
        return sb.ToString();
    }

    public IEnumerator<MikroTikResponseFrameDictionary> GetEnumerator() => _receivedFrames.GetEnumerator();

    public ReadOnlySpan<MikroTikResponseFrameDictionary> Span => CollectionsMarshal.AsSpan(_receivedFrames);

    IEnumerator IEnumerable.GetEnumerator() => _receivedFrames.GetEnumerator();

    [DebuggerNonUserCode]
    private sealed class TypeProxy
    {
        private readonly MikroTikResponse _self;
        public TypeProxy(MikroTikResponse self)
        {
            _self = self;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        [SuppressMessage("CodeQuality", "IDE0051:Удалите неиспользуемые закрытые члены", Justification = "Debug Display")]
        private Span<MikroTikResponseFrameDictionary> Items => CollectionsMarshal.AsSpan(_self._receivedFrames);
    }
}
