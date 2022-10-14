using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi;

public class MikroTikFlowCommand : MikroTikCommand
{
    private readonly MikroTikConnection _mtConnection;

    public MikroTikFlowCommand(string command, MikroTikConnection connection) : base(command)
    {
        ArgumentNullException.ThrowIfNull(connection);

        connection.CheckDisposed();
        _mtConnection = connection;
    }

    #region Query

    /// <summary>
    /// Добавляет уточнение вида "?query".
    /// </summary>
    public new MikroTikFlowCommand Query(string query)
    {
        base.Query(query);
        return this;
    }

    /// <summary>
    /// Добавляет уточнение вида "?query=value".
    /// </summary>
    public new MikroTikFlowCommand Query(string query, string value)
    {
        base.Query(query, value);
        return this;
    }

    /// <summary>
    /// Добавляет уточнение вида "?query=value1,value2,...".
    /// </summary>
    public new MikroTikFlowCommand Query(string query, params string[] values)
    {
        base.Query(query, values);
        return this;
    }

    #endregion

    #region Attribute

    /// <summary>
    /// Добавляет атрибут вида "=name=".
    /// </summary>
    public new MikroTikFlowCommand Attribute(string name)
    {
        base.Attribute(name);
        return this;
    }

    /// <summary>
    /// Добавляет атрибут вида "=name=value".
    /// </summary>
    public new MikroTikFlowCommand Attribute(string name, string value)
    {
        base.Attribute(name, value);
        return this;
    }

    /// <summary>
    /// Добавляет атрибут вида "=name=value1,value2,...".
    /// </summary>
    public new MikroTikFlowCommand Attribute(string name, params string[] values)
    {
        base.Attribute(name, values);
        return this;
    }

    /// <summary>
    /// Добавляет атрибут вида "=name=value1,value2,...".
    /// </summary>
    public new MikroTikFlowCommand Attribute(string name, IEnumerable<string> values)
    {
        base.Attribute(name, values);
        return this;
    }

    #endregion

    #region Proplist

    /// <summary>
    /// Добавляет атрибут вида "=.proplist=value1,value2,...".
    /// </summary>
    public new MikroTikFlowCommand Proplist(IEnumerable<string> values)
    {
        base.Proplist(values);
        return this;
    }

    /// <summary>
    /// Добавляет атрибут вида "=.proplist=value1,value2,...".
    /// </summary>
    public new MikroTikFlowCommand Proplist(params string[] values)
    {
        base.Proplist(values);
        return this;
    }

    #endregion

    #region Send

    /// <summary>
    /// Отправляет команду и возвращает ответ сервера.
    /// </summary>
    /// <exception cref="MikroApiTrapException"/>
    public MikroTikResponse Send(CancellationToken cancellationToken = default)
    {
        return _mtConnection.Execute(this, cancellationToken);
    }

    ///// <summary>
    ///// Отправляет команду и возвращает ответ сервера.
    ///// </summary>
    ///// <exception cref="MikroTikTrapException"/>
    //public Task<MikroTikResponse> SendAsync(CancellationToken cancellationToken) => _con.SendAsync(this, cancellationToken);

    #endregion

    #region Listen

    /// <summary>
    /// Отправляет команду помечая её тегом.
    /// Команда будет выполняться пока не будет прервана с помощью Cancel.
    /// </summary>
    /// <exception cref="MikroApiTrapException"/>
    public MikroTikResponseListener Listen(CancellationToken cancellationToken = default) => _mtConnection.Listen(this, cancellationToken);

    /// <summary>
    /// Отправляет команду помечая её тегом.
    /// Команда будет выполняться пока не будет прервана с помощью Cancel.
    /// </summary>
    public Task<MikroTikResponseListener> ListenAsync(CancellationToken cancellationToken = default) => _mtConnection.ListenAsync(this, cancellationToken);

    #endregion Listen

    #region Scalar

    /// <exception cref="InvalidOperationException"/>
    public string Scalar(CancellationToken cancellationToken = default) => _mtConnection.Execute(this, cancellationToken).Scalar();

    /// <exception cref="InvalidOperationException"/>
    public T? Scalar<T>(CancellationToken cancellationToken = default) => _mtConnection.Execute(this, cancellationToken).Scalar<T>();

    /// <exception cref="InvalidOperationException"/>
    public string Scalar(string columnName, CancellationToken cancellationToken = default) => _mtConnection.Execute(this, cancellationToken).Scalar(columnName);

    /// <exception cref="InvalidOperationException"/>
    public T? Scalar<T>(string columnName, CancellationToken cancellationToken = default) => _mtConnection.Execute(this, cancellationToken).Scalar<T>(columnName);

    #endregion

    #region ScalarArray

    /// <exception cref="InvalidOperationException"/>
    public string[] ScalarArray(CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarArray();

    /// <exception cref="InvalidOperationException"/>
    public T?[] ScalarArray<T>(CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarArray<T>();

    public string[] ScalarArray(string columnName, CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).ScalarArray(columnName);

    public T?[] ScalarArray<T>(string columnName, CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarArray<T>(columnName);

    #endregion

    #region ScalarList

    /// <exception cref="InvalidOperationException"/>
    public List<string> ScalarList(CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarList();

    /// <exception cref="InvalidOperationException"/>
    public List<T?> ScalarList<T>(CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarList<T>();

    public List<string> ScalarList(string columnName, CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarList(columnName);

    public List<T?> ScalarList<T>(string columnName, CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarList<T>(columnName);

    #endregion

    #region ScalarOrDefault

    /// <summary>
    /// Возвращает <see langword="null"/> если нет ни одной строки.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    public string? ScalarOrDefault(CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarOrDefault();

    /// <summary>
    /// Возвращает <see langword="default"/> если нет ни одной строки.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    public T? ScalarOrDefault<T>(CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarOrDefault<T>();

    /// <summary>
    /// Возвращает <see langword="null"/> если нет ни одной строки.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    public string? ScalarOrDefault(string columnName, CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).ScalarOrDefault(columnName);

    /// <summary>
    /// Возвращает <see langword="default"/> если нет ни одной строки.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    public T? ScalarOrDefault<T>(string columnName, CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ScalarOrDefault<T>(columnName);

    #endregion

    #region Single

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public MikroTikResponseFrameDictionary Single(CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).Single();

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public T Single<T>(Func<MikroTikResponseFrameDictionary, T> selector, CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).Single(selector);

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public T Single<T>(CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).Single<T>();

    #endregion

    #region SingleOrDefault

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public MikroTikResponseFrameDictionary? SingleOrDefault(CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).SingleOrDefault();

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public T? SingleOrDefault<T>(Func<MikroTikResponseFrameDictionary, T> selector, CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).SingleOrDefault(selector);

    /// <summary>
    /// Когда результатом является одна строка.
    /// </summary>
    public T? SingleOrDefault<T>(CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).SingleOrDefault<T>();

    #endregion

    #region ToArray

    /// <summary>
    /// Создает список объектов
    /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T[] ToArray<T>(CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).ToArray<T>();

    public T[] ToArray<T>(Func<MikroTikResponseFrameDictionary, T> selector, CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).ToArray(selector);

    /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
    public T[] ToArray<T>(T anonymousObject, CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).ToArray(anonymousObject);

    #endregion

    #region ToList

    /// <summary>
    /// Создает список объектов
    /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public List<T> ToList<T>(CancellationToken cancellationToken = default) => 
        _mtConnection.Execute(this, cancellationToken).ToList<T>();

    public List<T> ToList<T>(Func<MikroTikResponseFrameDictionary, T> selector, CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).ToList(selector);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
    /// <returns></returns>
    public List<T> ToList<T>(T anonymousObject, CancellationToken cancellationToken = default) =>
        _mtConnection.Execute(this, cancellationToken).ToList(anonymousObject);

    #endregion

    #region Async

    /// <summary>
    /// Отправляет команду и возвращает ответ сервера.
    /// </summary>
    /// <exception cref="MikroApiTrapException"/>
    /// <exception cref="MikroApiFatalException"/>
    /// <exception cref="MikroApiDisconnectException"/>
    public Task<MikroTikResponse> SendAsync(CancellationToken cancellationToken = default) =>
        _mtConnection.ExecuteAsync(this, cancellationToken);

    public async Task<string> ScalarAsync(CancellationToken cancellationToken = default)
    {
        var result = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return result.Scalar();
    }

    public async Task<T?> ScalarAsync<T>(CancellationToken cancellationToken = default)
    {
        var result = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return result.Scalar<T>();
    }

    public async Task<string> ScalarAsync(string columnName, CancellationToken cancellationToken = default)
    {
        var result = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return result.Scalar(columnName);
    }

    public async Task<T?> ScalarAsync<T>(string columnName, CancellationToken cancellationToken = default)
    {
        var result = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return result.Scalar<T>(columnName);
    }

    public async Task<List<string>> ScalarListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ScalarList();
    }

    public async Task<List<T?>> ScalarListAsync<T>(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ScalarList<T>();
    }

    public async Task<List<string>> ScalarListAsync(string columnName, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ScalarList(columnName);
    }

    public async Task<List<T?>> ScalarListAsync<T>(string columnName, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ScalarList<T>(columnName);
    }

    public async Task<string?> ScalarOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ScalarOrDefault();
    }

    public async Task<T?> ScalarOrDefaultAsync<T>(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ScalarOrDefault<T>();
    }

    public async Task<string?> ScalarOrDefaultAsync(string columnName, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ScalarOrDefault(columnName);
    }

    public async Task<T?> ScalarOrDefaultAsync<T>(string columnName, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ScalarOrDefault<T>(columnName);
    }

    public async Task<MikroTikResponseFrameDictionary> SingleAsync(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.Single();
    }

    public async Task<T> SingleAsync<T>(Func<MikroTikResponseFrameDictionary, T> selector, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.Single(selector);
    }

    public async Task<T> SingleAsync<T>(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.Single<T>();
    }

    public async Task<MikroTikResponseFrameDictionary?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.SingleOrDefault();
    }

    public async Task<T?> SingleOrDefaultAsync<T>(Func<MikroTikResponseFrameDictionary, T> selector, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.SingleOrDefault(selector);
    }

    public async Task<T?> SingleOrDefaultAsync<T>(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.SingleOrDefault<T>();
    }

    public async Task<T[]> ToArrayAsync<T>(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ToArray<T>();
    }

    public async Task<T[]> ToArrayAsync<T>(Func<MikroTikResponseFrameDictionary, T> selector, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ToArray(selector);
    }

    public async Task<T[]> ToArrayAsync<T>(T anonymousObject, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ToArray(anonymousObject);
    }

    public async Task<List<T>> ToListAsync<T>(CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ToList<T>();
    }

    public async Task<List<T>> ToListAsync<T>(Func<MikroTikResponseFrameDictionary, T> selector, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ToList(selector);
    }

    public async Task<List<T>> ToListAsync<T>(T anonymousObject, CancellationToken cancellationToken = default)
    {
        var response = await _mtConnection.ExecuteAsync(this, cancellationToken).ConfigureAwait(false);
        return response.ToList(anonymousObject);
    }

    #endregion
}
