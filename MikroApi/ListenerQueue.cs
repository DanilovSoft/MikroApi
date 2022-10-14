using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static DanilovSoft.MikroApi.ExceptionMessages;

namespace DanilovSoft.MikroApi.Threading;

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
    /// <exception cref="TimeoutException"/>
    public T Take(int millisecondsTimeout = Timeout.Infinite, CancellationToken cancellationToken = default)
    {
        var valueTask = TakeAsync(millisecondsTimeout, cancellationToken);
        if (valueTask.IsCompletedSuccessfully)
        {
            return valueTask.Result;
        }

        return valueTask
            .AsTask()
            .WaitAsync(cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Ожидает получения ответа от сервера.
    /// </summary>
    /// <exception cref="TimeoutException"/>
    public async ValueTask<T> TakeAsync(int millisecondsTimeout = Timeout.Infinite, CancellationToken cancellationToken = default)
    {
        var item = await TakeCore(millisecondsTimeout, cancellationToken).ConfigureAwait(false);
        return item.GetResult();
    }

    /// <summary>
    /// Возвращает объект из коллекции.
    /// </summary>
    public bool TryTake([NotNullWhen(true)] out T? value)
    {
        if (_channel.Reader.TryRead(out var queueResult))
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

    private async ValueTask<QueueResult<T>> TakeCore(int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if (millisecondsTimeout == Timeout.Infinite)
        {
            return await TakeCore(cancellationToken).ConfigureAwait(false);
        }

        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            linkedCts.CancelAfter(millisecondsTimeout);
            try
            {
                return await TakeCore(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(string.Format(CultureInfo.InvariantCulture, TimeoutMessage, millisecondsTimeout));
            }
        }
    }

    private async ValueTask<QueueResult<T>> TakeCore(CancellationToken cancellationToken)
    {
        try
        {
            return await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
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
