using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static DanilovSoft.MikroApi.ExceptionMessages;

namespace DanilovSoft.MikroApi.Threading
{
    /// <summary>
    /// Принимает и отдаёт фреймы тегированных сообщений.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay(@"\{{_channel}\}")]
    internal sealed class ListenerQueue<T> where T : notnull
    {
        private readonly Channel<QueueResult<T>> _channel;

        // ctor
        public ListenerQueue()
        {
            _channel = Channel.CreateUnbounded<QueueResult<T>>(new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = false,
                SingleReader = false,
                SingleWriter = true,
            });
        }

        /// <summary>
        /// <see langword="true"/> если был получен "!done" или "CriticalException".
        /// </summary>
        internal bool IsDone => _channel.Reader.Completion.IsCompleted;

        /// <summary>
        /// Ожидает получения ответа от сервера.
        /// </summary>
        public T Take()
        {
            return Take(millisecondsTimeout: -1, CancellationToken.None);
        }

        /// <summary>
        /// Ожидает получения ответа от сервера.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        public T Take(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var task = TakeAsync(millisecondsTimeout, cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                return task.Result;
            }
            else
            {
                return task.AsTask().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Ожидает получения ответа от сервера.
        /// </summary>
        public ValueTask<T> TakeAsync()
        {
            return TakeAsync(millisecondsTimeout: -1, CancellationToken.None);
        }

        /// <summary>
        /// Ожидает получения ответа от сервера.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        public async ValueTask<T> TakeAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            do
            {
                QueueResult<T> item;
                using (var timeoutCts = new CancellationTokenSource(millisecondsTimeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                {
                    try
                    {
                        item = await _channel.Reader.ReadAsync(linkedCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw new TimeoutException(string.Format(CultureInfo.InvariantCulture, TimeoutMessage, millisecondsTimeout));
                    }
                    catch (ChannelClosedException ex)
                    {
                        if (ex.InnerException != null)
                        {
                            throw ex.InnerException;
                        }

                        throw;
                    }
                }

                return item.GetResult();

            } while (true);
        }

        /// <summary>
        /// Возвращает объект из коллекции.
        /// </summary>
        public bool TryTake([NotNullWhen(true)] out T? value)
        {
            if (_channel.Reader.TryRead(out QueueResult<T> queueResult))
            {
                value = queueResult.GetResult();
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Добавляет результат в коллекцию.
        /// Опосредованно вызывается потоком читающим из сокета.
        /// </summary>
        private void InnerAdd(QueueResult<T> result)
        {
            _channel.Writer.TryWrite(result);
        }

        /// <summary>
        /// Добавляет результат в коллекцию.
        /// Вызывается потоком читающим из сокета.
        /// </summary>
        public void AddResult(T result)
        {
            InnerAdd(new QueueResult<T>(result));
        }

        /// <summary>
        /// Добавляет исключение как результат в коллекцию.
        /// Вызывается потоком читающим из сокета.
        /// </summary>
        /// <param name="trapException">Результат.</param>
        public void AddTrap(MikroApiTrapException trapException)
        {
            if (trapException.Category == TrapCategory.ExecutionOfCommandInterrupted)
            {
                // Операция грациозно прервана. Переопределить исключение что-бы пользователь
                // мог понять что операция отменена грациозно.
                InnerAdd(new QueueResult<T>(new MikroApiCommandInterruptedException(trapException.Message)));
            }
            else
            {
                InnerAdd(new QueueResult<T>(trapException));
            }
        }

        /// <summary>
        /// Добавляет исключение как результат в коллекцию.
        /// Вызывается потоком читающим из сокета.
        /// </summary>
        public void AddFatal(Exception exception)
        {
            InnerAdd(new QueueResult<T>(exception));
        }

        /// <summary>
        /// Потокобезопасно добавляет исключение как результат в коллекцию и запрещает добавлять новые результаты.
        /// Вызывается потоком читающим из сокета или отправляющим в сокет.
        /// </summary>
        public void AddCriticalException(Exception exception)
        {
            InnerAdd(new QueueResult<T>(exception));

            // Больше не будет сообщений.
            _channel.Writer.TryComplete(new InvalidOperationException(CompletedException));
        }

        // Получен "!done". Вызывается потоком читающим из сокета.
        public void Done()
        {
            // Больше не будет сообщений.
            _channel.Writer.TryComplete(new MikroApiDoneException());
        }
    }
}
