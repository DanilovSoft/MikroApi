using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    public class MikroTikFlowCommand : MikroTikCommand
    {
        private readonly MikroTikConnection _mtConnection;

        // ctor
        public MikroTikFlowCommand(string command, MikroTikConnection connection) : base(command)
        {
            _mtConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        #region Query

        /// <summary>
        /// Добавляет уточнение вида "?query"
        /// </summary>
        public new MikroTikFlowCommand Query(string query)
        {
            base.Query(query);
            return this;
        }

        /// <summary>
        /// Добавляет уточнение вида "?query=value"
        /// </summary>
        public new MikroTikFlowCommand Query(string query, string value)
        {
            base.Query(query, value);
            return this;
        }

        /// <summary>
        /// Добавляет уточнение вида "?query=value1,value2,..."
        /// </summary>
        public new MikroTikFlowCommand Query(string query, params string[] values)
        {
            base.Query(query, values);
            return this;
        }

        #endregion

        #region Attribute

        /// <summary>
        /// Добавляет атрибут вида "=name="
        /// </summary>
        public new MikroTikFlowCommand Attribute(string name)
        {
            base.Attribute(name);
            return this;
        }

        /// <summary>
        /// Добавляет атрибут вида "=name=value"
        /// </summary>
        public new MikroTikFlowCommand Attribute(string name, string value)
        {
            base.Attribute(name, value);
            return this;
        }

        /// <summary>
        /// Добавляет атрибут вида "=name=value1,value2,..."
        /// </summary>
        public new MikroTikFlowCommand Attribute(string name, params string[] values)
        {
            base.Attribute(name, values);
            return this;
        }

        /// <summary>
        /// Добавляет атрибут вида "=name=value1,value2,..."
        /// </summary>
        public new MikroTikFlowCommand Attribute(string name, IEnumerable<string> values)
        {
            base.Attribute(name, values);
            return this;
        }

        #endregion

        #region Proplist

        /// <summary>
        /// Добавляет атрибут вида "=.proplist=value1,value2,..."
        /// </summary>
        public new MikroTikFlowCommand Proplist(IEnumerable<string> values)
        {
            base.Proplist(values);
            return this;
        }

        /// <summary>
        /// Добавляет атрибут вида "=.proplist=value1,value2,..."
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
        /// <exception cref="MikroTikTrapException"/>
        public MikroTikResponse Send()
        {
            return _mtConnection.Send(this);
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
        /// <exception cref="MikroTikTrapException"/>
        public MikroTikResponseListener Listen() => _mtConnection.Listen(this);

        /// <summary>
        /// Отправляет команду помечая её тегом.
        /// Команда будет выполняться пока не будет прервана с помощью Cancel.
        /// </summary>
        public Task<MikroTikResponseListener> ListenAsync() => _mtConnection.ListenAsync(this);

        #endregion Listen

        #region Scalar

        /// <exception cref="InvalidOperationException"/>
        public string Scalar() => _mtConnection.Send(this).Scalar();

        /// <exception cref="InvalidOperationException"/>
        public T Scalar<T>() => _mtConnection.Send(this).Scalar<T>();

        /// <exception cref="InvalidOperationException"/>
        public string Scalar(string columnName) => _mtConnection.Send(this).Scalar(columnName);

        /// <exception cref="InvalidOperationException"/>
        public T Scalar<T>(string columnName) => _mtConnection.Send(this).Scalar<T>(columnName);

        #endregion

        #region ScalarArray

        /// <exception cref="InvalidOperationException"/>
        public string[] ScalarArray() => _mtConnection.Send(this).ScalarArray();

        /// <exception cref="InvalidOperationException"/>
        public T[] ScalarArray<T>() => _mtConnection.Send(this).ScalarArray<T>();

        public string[] ScalarArray(string columnName) => _mtConnection.Send(this).ScalarArray(columnName);

        public T[] ScalarArray<T>(string columnName) => _mtConnection.Send(this).ScalarArray<T>(columnName);

        #endregion

        #region ScalarList

        /// <exception cref="InvalidOperationException"/>
        public List<string> ScalarList() => _mtConnection.Send(this).ScalarList();

        /// <exception cref="InvalidOperationException"/>
        public List<T> ScalarList<T>() => _mtConnection.Send(this).ScalarList<T>();

        public List<string> ScalarList(string columnName) => _mtConnection.Send(this).ScalarList(columnName);

        public List<T> ScalarList<T>(string columnName) => _mtConnection.Send(this).ScalarList<T>(columnName);

        #endregion

        #region ScalarOrDefault

        /// <summary>
        /// Возвращает <see langword="null"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        public string? ScalarOrDefault() => _mtConnection.Send(this).ScalarOrDefault();

        /// <summary>
        /// Возвращает <see langword="default"/> если нет ни одной строки.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        public T? ScalarOrDefault<T>() => _mtConnection.Send(this).ScalarOrDefault<T>();

        /// <summary>
        /// Возвращает <see langword="null"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        public string? ScalarOrDefault(string columnName) => _mtConnection.Send(this).ScalarOrDefault(columnName);

        /// <summary>
        /// Возвращает <see langword="default"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        public T? ScalarOrDefault<T>(string columnName) => _mtConnection.Send(this).ScalarOrDefault<T>(columnName);

        #endregion

        #region Single

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public MikroTikResponseFrame Single() => _mtConnection.Send(this).Single();

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public T Single<T>(Func<MikroTikResponseFrame, T> selector) => _mtConnection.Send(this).Single(selector);

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public T Single<T>() => _mtConnection.Send(this).Single<T>();

        #endregion

        #region SingleOrDefault

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public MikroTikResponseFrame? SingleOrDefault() => _mtConnection.Send(this).SingleOrDefault();

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public T? SingleOrDefault<T>(Func<MikroTikResponseFrame, T> selector) => _mtConnection.Send(this).SingleOrDefault(selector);

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public T? SingleOrDefault<T>() => _mtConnection.Send(this).SingleOrDefault<T>();

        #endregion

        #region ToArray

        /// <summary>
        /// Создает список объектов
        /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] ToArray<T>() => _mtConnection.Send(this).ToArray<T>();

        public T[] ToArray<T>(Func<MikroTikResponseFrame, T> selector) => _mtConnection.Send(this).ToArray<T>(selector);

        /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
        public T[] ToArray<T>(T anonymousObject) => _mtConnection.Send(this).ToArray<T>(anonymousObject);

        #endregion

        #region ToList

        /// <summary>
        /// Создает список объектов
        /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> ToList<T>() => _mtConnection.Send(this).ToList<T>();

        public List<T> ToList<T>(Func<MikroTikResponseFrame, T> selector) => _mtConnection.Send(this).ToList<T>(selector);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
        /// <returns></returns>
        public List<T> ToList<T>(T anonymousObject) => _mtConnection.Send(this).ToList<T>(anonymousObject);

        #endregion

        #region IAsyncResponse

        /// <summary>
        /// Отправляет команду и возвращает ответ сервера.
        /// </summary>
        /// <exception cref="MikroTikTrapException"/>
        /// <exception cref="MikroTikFatalException"/>
        /// <exception cref="MikroTikDisconnectException"/>
        public Task<MikroTikResponse> SendAsync() => _mtConnection.SendAsync(this);

        public async Task<string> ScalarAsync()
        {
            var result = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return result.Scalar();
        }

        public async Task<T> ScalarAsync<T>()
        {
            var result = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return result.Scalar<T>();
        }

        public async Task<string> ScalarAsync(string columnName)
        {
            var result = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return result.Scalar(columnName);
        }

        public async Task<T> ScalarAsync<T>(string columnName)
        {
            var result = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return result.Scalar<T>(columnName);
        }

        public async Task<List<string>> ScalarListAsync()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ScalarList();
        }

        public async Task<List<T>> ScalarListAsync<T>()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ScalarList<T>();
        }

        public async Task<List<string>> ScalarListAsync(string columnName)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ScalarList(columnName);
        }

        public async Task<List<T>> ScalarListAsync<T>(string columnName)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ScalarList<T>(columnName);
        }

        public async Task<string?> ScalarOrDefaultAsync()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ScalarOrDefault();
        }

        public async Task<T?> ScalarOrDefaultAsync<T>()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ScalarOrDefault<T>();
        }

        public async Task<string?> ScalarOrDefaultAsync(string columnName)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ScalarOrDefault(columnName);
        }

        public async Task<T?> ScalarOrDefaultAsync<T>(string columnName)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ScalarOrDefault<T>(columnName);
        }

        public async Task<MikroTikResponseFrame> SingleAsync()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.Single();
        }

        public async Task<T> SingleAsync<T>(Func<MikroTikResponseFrame, T> selector)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.Single<T>(selector);
        }

        public async Task<T> SingleAsync<T>()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.Single<T>();
        }

        public async Task<MikroTikResponseFrame?> SingleOrDefaultAsync()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.SingleOrDefault();
        }

        public async Task<T?> SingleOrDefaultAsync<T>(Func<MikroTikResponseFrame, T> selector)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.SingleOrDefault(selector);
        }

        public async Task<T?> SingleOrDefaultAsync<T>()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.SingleOrDefault<T>();
        }

        public async Task<T[]> ToArrayAsync<T>()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ToArray<T>();
        }

        public async Task<T[]> ToArrayAsync<T>(Func<MikroTikResponseFrame, T> selector)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ToArray<T>(selector);
        }

        public async Task<T[]> ToArrayAsync<T>(T anonymousObject)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ToArray<T>(anonymousObject);
        }

        public async Task<List<T>> ToListAsync<T>()
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ToList<T>();
        }

        public async Task<List<T>> ToListAsync<T>(Func<MikroTikResponseFrame, T> selector)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ToList<T>(selector);
        }

        public async Task<List<T>> ToListAsync<T>(T anonymousObject)
        {
            var response = await _mtConnection.SendAsync(this).ConfigureAwait(false);
            return response.ToList<T>(anonymousObject);
        }

        #endregion
    }
}
