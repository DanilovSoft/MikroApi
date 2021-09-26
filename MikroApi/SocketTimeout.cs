using System;
using System.Threading;

namespace DanilovSoft.MikroApi
{
    internal sealed class SocketTimeout : IDisposable
    {
        private readonly object _syncTimer = new();
        private readonly Timer _watchdogTimer;
        private readonly TimerCallback _timerCallback;
        private readonly Slot _slot;
        private readonly int _millisecondsTimeout;
        private int _state;
        private bool _disposed;

        public SocketTimeout(TimerCallback timerCallback, int millisecondsTimeout)
        {
            _slot = new Slot(this);
            _millisecondsTimeout = millisecondsTimeout;
            _watchdogTimer = new Timer(OnTimer);
            _timerCallback = timerCallback;
        }

        // Поток сначала вызывает Start
        internal Slot Start()
        {
            // Запланировать таймер если он остановлен.
            Interlocked.CompareExchange(ref _state, 1, 0);

            lock (_syncTimer)
            {
                if (!_disposed)
                {
                    // Запланировать сработать один раз.
                    _watchdogTimer.Change(_millisecondsTimeout, -1);
                }
            }

            return _slot;
        }

        // Затем поток вызывает Stop
        /// <summary>
        /// Попытка остановить таймер.
        /// </summary>
        /// <exception cref="MikroApiConnectionClosedAbnormallyException"/>
        public void Stop()
        {
            // Остановить таймер если он запланирован.
            int state = Interlocked.CompareExchange(ref _state, 0, 1);

            if (state != 2)
            {
                lock (_syncTimer)
                {
                    // Stop мог сработать из-за вызова Dispose пользователем на верхнем уровне.
                    if (!_disposed)
                    {
                        // Остановить таймер.
                        _watchdogTimer.Change(-1, -1);
                    }
                }
            }
            else
            // Не успели отменить таймер.
            {
                throw new MikroApiConnectionClosedAbnormallyException();
            }
        }

        private void OnTimer(object? timerState)
        {
            // Установить состояние 'Сработал' если состояние было 'Запланирован'
            int state = Interlocked.CompareExchange(ref _state, 2, 1);

            if (state == 1)
            // Таймер сработал.
            {
                _timerCallback(timerState);
            }

            // Состояние таймера было 'Остановлен'
        }

        public void Dispose()
        {
            lock (_syncTimer)
            {
                if (!_disposed)
                {
                    _watchdogTimer.Dispose();
                    _disposed = true;
                }
            }
        }

        internal sealed class Slot : IDisposable
        {
            private readonly SocketTimeout _self;

            public Slot(SocketTimeout parent)
            {
                _self = parent;
            }

            public void Dispose()
            {
                _self.Stop();
            }
        }
    }
}
