using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DanilovSoft.MikroApi.Helpers;

namespace DanilovSoft.MikroApi;

/// <summary>
/// Словарь подписчиков на ответы сервера с разными тегами. Потокобезопасный.
/// </summary>
[DebuggerDisplay("{DebugDisplay,nq}")]
[DebuggerTypeProxy(typeof(TypeProxy))]
internal class ResponseListeners
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugDisplay => "{" + $"Count = {_dict.Count}" + "}";
    private readonly Dictionary<string, IMikroTikResponseListener> _dict = new();
    /// <summary>
    /// Представляет собой исключение типа !fatal
    /// или исключение типа обрыв соединения.
    /// </summary>
    private Exception? _fatalException;

    // ctor
    internal ResponseListeners()
    {

    }

    /// <summary>
    /// Добавляет подписчика в словарь с определенным тегом.
    /// </summary>
    /// <param name="listener">Подписчик</param>
    /// <exception cref="InvalidOperationException"/>
    internal void AddQuit(IMikroTikResponseListener listener)
    {
        lock (_dict)
        {
            ThrowIfFatal();
            ThrowIfMaxCount();

            ref var listenerRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_dict, "", out var exists);
            if (!exists)
            {
                listenerRef = listener;
            }
            else
            {
                ThrowHelper.QuitAlreadyInProcess();
            }
        }
    }

    /// <summary>
    /// Добавляет подписчика в словарь.
    /// </summary>
    /// <exception cref="Exception"/>
    internal void Add(string tag, IMikroTikResponseListener listener)
    {
        lock (_dict)
        {
            ThrowIfFatal();
            ThrowIfMaxCount();

            _dict.Add(tag, listener);
        }
    }

    /// <exception cref="InvalidOperationException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfMaxCount()
    {
        if (_dict.Count < ushort.MaxValue)
        {
            return;
        }

        ThrowHelper.ThrowMaxListeners();
    }

    /// <summary>
    /// Бросает исключение если _fatalException не NULL
    /// </summary>
    /// <exception cref="Exception"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfFatal()
    {
        if (_fatalException == null)
        {
            return;
        }

        ThrowHelper.ThrowDelegatedError(_fatalException);
    }

    /// <summary>
    /// Потокобезопасно добавляет исключение всем подписчикам и удаляет их из словаря.
    /// Вызывается потоком читающим из сокета или отправляющим в сокет.
    /// </summary>
    internal void AddCriticalException(Exception exception, bool gotFatal)
    {
        lock (_dict)
        {
            if (_fatalException == null)
            {
                if (_dict.Count > 0)
                {
                    foreach (var listener in _dict.Values)
                    {
                        lock (listener.SyncObj)
                        {
                            if (gotFatal)
                            {
                                listener.AddFatal(exception);
                            }
                            else
                            {
                                listener.AddCriticalException(exception);
                            }
                        }
                    }
                    _dict.Clear();
                }

                // В этот словарь больше нельзя добавлять подписчиков.
                _fatalException = exception;
            }
        }
    }

    /// <summary>
    /// Удаляет подписчика из словаря. Потокобезопасно.
    /// </summary>
    internal void Remove(string tag)
    {
        lock (_dict)
        {
            _dict.Remove(tag);
        }
    }

    internal bool TryGetValue(string tag, [MaybeNullWhen(false)] out IMikroTikResponseListener listener)
    {
        lock (_dict)
        {
            return _dict.TryGetValue(tag, out listener);
        }
    }

    private sealed class TypeProxy
    {
        private readonly ResponseListeners _self;
        public TypeProxy(ResponseListeners self)
        {
            _self = self;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Dictionary<string, IMikroTikResponseListener> Items => _self._dict;
    }
}
