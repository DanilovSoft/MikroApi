using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using DanilovSoft.MikroApi.Helpers;

namespace DanilovSoft.MikroApi;

internal sealed class SocketTimeout : IDisposable
{
    private readonly object _syncTimer = new();
    private readonly TimerCallback _callback;
    private readonly ReusableWatchdog _reusable;
    private readonly int _millisecondsTimeout;
    private Timer? _watchdogTimer;
    private int _state;

    public SocketTimeout(TimerCallback callback, int millisecondsTimeout)
    {
        _reusable = new ReusableWatchdog(this);
        _millisecondsTimeout = millisecondsTimeout;
        _watchdogTimer = new Timer(OnTimer);
        _callback = callback;
    }

    public void Dispose()
    {
        lock (_syncTimer)
        {
            if (_watchdogTimer != null)
            {
                NullableHelper.SetNull(ref _watchdogTimer).Dispose();
            }
        }
    }

    // 1) Поток сначала вызывает Start.
    /// <exception cref="ObjectDisposedException"/>
    internal ReusableWatchdog StartWatchdog()
    {
        CheckDisposed();

        // Запланировать таймер если он остановлен.
        Interlocked.CompareExchange(ref _state, 1, 0);

        lock (_syncTimer)
        {
            // Запланировать сработать один раз.
            _watchdogTimer?.Change(_millisecondsTimeout, -1);
        }

        return _reusable;
    }

    // 2) Затем поток вызывает Stop.
    /// <summary>
    /// Останавливает таймер или бросает исключение.
    /// </summary>
    /// <remarks>Потокобезопасный метод.</remarks>
    /// <exception cref="MikroApiConnectionClosedAbnormallyException"/>
    internal void StopWatchdog()
    {
        // Остановить таймер если он запланирован.
        var state = Interlocked.CompareExchange(ref _state, 0, 1);
        if (state != 2)
        {
            lock (_syncTimer)
            {
                // Stop мог сработать из-за вызова Close пользователем на верхнем уровне.
                // Остановить таймер.
                _watchdogTimer?.Change(-1, -1);
            }
        }
        else
        // Не успели отменить таймер.
        {
            ThrowClosedAbnormally();
        }
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowClosedAbnormally()
    {
        throw new MikroApiConnectionClosedAbnormallyException();
    }

    private void OnTimer(object? timerState)
    {
        // Установить состояние 'Сработал' если состояние было 'Запланирован'
        var state = Interlocked.CompareExchange(ref _state, 2, 1);
        if (state == 1)
        // Произошёл таймаут по таймеру.
        {
            _callback(timerState);
        }
        else
        {
            // Таймер уже успели остановить.
        }
    }

    /// <exception cref="ObjectDisposedException"/>
    [MemberNotNull(nameof(_watchdogTimer))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        if (_watchdogTimer != null)
        {
            return;
        }

        ThrowHelper.ThrowDisposed<SocketTimeout>();
    }

    internal sealed class ReusableWatchdog
    {
        private readonly SocketTimeout _self;

        public ReusableWatchdog(SocketTimeout parent)
        {
            _self = parent;
        }

        public void StopTimer()
        {
            _self.StopWatchdog();
        }
    }
}
