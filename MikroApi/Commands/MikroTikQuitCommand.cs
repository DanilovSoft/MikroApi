using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace DanilovSoft.MikroApi;

/*
>>> /quit

<<< !fatal
<<< session terminated on request

 */

internal class MikroTikQuitCommand : MikroTikCommand, IMikroTikResponseListener
{
    public const string ExceptionDebugMessage = "Got exception on MikroTik '/quit' command: {0}";
    // Это свойство требуется интерфейсом но не участвует в синхронизации потоков.
    private readonly object _syncObj = new();
    object IMikroTikResponseListener.SyncObj => _syncObj;
    /// <summary>
    /// Исключение типа обрыв соединения.
    /// </summary>
    private volatile Exception? _criticalException;

    internal MikroTikQuitCommand() : base("/quit")
    {

    }

    /// <summary>
    /// Возвращает <see langword="true"/> если был получен !fatal.
    /// Не бросает исключения. Вместо этого возвращает <see langword="false"/>.
    /// </summary>
    /// <param name="millisecondsTimeout"></param>
    internal bool Wait(int millisecondsTimeout)
    {
        // Разрешаем вход другому потоку и ждем пока он сообщит о завершении.
        var success = Monitor.Wait(this, millisecondsTimeout);
        if (success)
        {
            var ex = _criticalException;
            if (ex != null)
            {
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, ExceptionDebugMessage, ex.Message));
                return false;
            }
        }
        return success;
    }

    void IMikroTikResponseListener.AddFatal(Exception exception)
    {
        ReleaseMonitor();
    }

    // !fatal получим здесь от потока читающего из сокета.
    // Сервер мог закрыть соединение быстрее чем мы прочитаем !fatal.
    void IMikroTikResponseListener.AddCriticalException(Exception exception)
    {
        _criticalException = exception;
        ReleaseMonitor();
    }

    private void ReleaseMonitor()
    {
        ThreadPool.UnsafeQueueUserWorkItem(state =>
        {
            lock (state)
            {
                // Сообщаем родителю что подтверждение получено.
                Monitor.Pulse(state);
            }
        }, this, preferLocal: true);
    }

    #region Не используемые члены интерфейса
    // Не может произойти.
    void IMikroTikResponseListener.AddResult(MikroTikResponseFrameDictionary message) { }
    // Не может произойти.
    void IMikroTikResponseListener.AddTrap(MikroApiTrapException trapException) { }
    // Не может произойти.
    void IMikroTikResponseListener.Done() { }
    #endregion
}
