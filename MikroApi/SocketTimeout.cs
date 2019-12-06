using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DanilovSoft.MikroApi
{
    internal sealed class SocketTimeout : IDisposable
    {
        private readonly Timer _timer;
        private readonly TimerCallback _timerCallback;
        private readonly Slot _slot;
        private readonly int _millisecondsTimeout;
        private int _state = 0;
        private bool _disposed;

        public SocketTimeout(TimerCallback timerCallback, int millisecondsTimeout)
        {
            _slot = new Slot(this);
            _millisecondsTimeout = millisecondsTimeout;
            _timer = new Timer(OnTimer);
            _timerCallback = timerCallback;
        }

        // Поток сначала вызывает Start
        internal Slot Start()
        {
            // Запланировать таймер если он остановлен.
            Interlocked.CompareExchange(ref _state, 1, 0);

            lock (_timer)
            {
                if (!_disposed)
                {
                    // Запланировать сработать один раз.
                    _timer.Change(_millisecondsTimeout, -1);
                }
            }

            return _slot;
        }

        // Затем поток вызывает Stop
        /// <summary>
        /// Попытка остановить таймер.
        /// </summary>
        /// <exception cref="MikroTikConnectionClosedAbnormally"/>
        public void Stop()
        {
            // Остановить таймер если он запланирован.
            int state = Interlocked.CompareExchange(ref _state, 0, 1);

            if (state != 2)
            {
                lock (_timer)
                {
                    // Stop мог сработать из-за вызова Dispose пользователем на верхнем уровне.
                    if (!_disposed)
                    {
                        // Остановить таймер.
                        _timer.Change(-1, -1);
                    }
                }
            }
            else
            // Не успели отменить таймер.
            {
                throw new MikroTikConnectionClosedAbnormally();
            }
        }

        private void OnTimer(object timerState)
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
            lock (_timer)
            {
                if (!_disposed)
                {
                    _timer.Dispose();
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
