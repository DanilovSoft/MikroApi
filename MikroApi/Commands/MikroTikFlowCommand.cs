using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    public class MikroTikFlowCommand : MikroTikCommand, IAsyncResponse
    {
        private readonly MikroTikConnection _con;

        // ctor
        public MikroTikFlowCommand(string command, MikroTikConnection connection) : base(command)
        {
            _con = connection ?? throw new ArgumentNullException(nameof(connection));
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
        public MikroTikResponse Send() => _con.Send(this);

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
        public MikroTikResponseListener Listen() => _con.Listen(this);

        /// <summary>
        /// Отправляет команду помечая её тегом.
        /// Команда будет выполняться пока не будет прервана с помощью Cancel.
        /// </summary>
        public Task<MikroTikResponseListener> ListenAsync() => _con.ListenAsync(this);

        #endregion Listen

        #region Scalar

        /// <exception cref="InvalidOperationException"/>
        public string Scalar() => _con.Send(this).Scalar();

        /// <exception cref="InvalidOperationException"/>
        public T Scalar<T>() => _con.Send(this).Scalar<T>();

        /// <exception cref="InvalidOperationException"/>
        public string Scalar(string columnName) => _con.Send(this).Scalar(columnName);

        /// <exception cref="InvalidOperationException"/>
        public T Scalar<T>(string columnName) => _con.Send(this).Scalar<T>(columnName);

        #endregion

        #region ScalarArray

        /// <exception cref="InvalidOperationException"/>
        public string[] ScalarArray() => _con.Send(this).ScalarArray();

        /// <exception cref="InvalidOperationException"/>
        public T[] ScalarArray<T>() => _con.Send(this).ScalarArray<T>();

        public string[] ScalarArray(string columnName) => _con.Send(this).ScalarArray(columnName);

        public T[] ScalarArray<T>(string columnName) => _con.Send(this).ScalarArray<T>(columnName);

        #endregion

        #region ScalarList

        /// <exception cref="InvalidOperationException"/>
        public List<string> ScalarList() => _con.Send(this).ScalarList();

        /// <exception cref="InvalidOperationException"/>
        public List<T> ScalarList<T>() => _con.Send(this).ScalarList<T>();

        public List<string> ScalarList(string columnName) => _con.Send(this).ScalarList(columnName);

        public List<T> ScalarList<T>(string columnName) => _con.Send(this).ScalarList<T>(columnName);

        #endregion

        #region ScalarOrDefault

        /// <summary>
        /// Возвращает <see langword="null"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        public string ScalarOrDefault() => _con.Send(this).ScalarOrDefault();

        /// <summary>
        /// Возвращает <see langword="default"/> если нет ни одной строки.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        public T ScalarOrDefault<T>() => _con.Send(this).ScalarOrDefault<T>();

        /// <summary>
        /// Возвращает <see langword="null"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        public string ScalarOrDefault(string columnName) => _con.Send(this).ScalarOrDefault(columnName);

        /// <summary>
        /// Возвращает <see langword="default"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        public T ScalarOrDefault<T>(string columnName) => _con.Send(this).ScalarOrDefault<T>(columnName);

        #endregion

        #region Single

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public MikroTikResponseFrame Single() => _con.Send(this).Single();

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public T Single<T>(Func<MikroTikResponseFrame, T> selector) => _con.Send(this).Single(selector);

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public T Single<T>() => _con.Send(this).Single<T>();

        #endregion

        #region SingleOrDefault

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public MikroTikResponseFrame SingleOrDefault() => _con.Send(this).SingleOrDefault();

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public T SingleOrDefault<T>(Func<MikroTikResponseFrame, T> selector) => _con.Send(this).SingleOrDefault(selector);

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        public T SingleOrDefault<T>() => _con.Send(this).SingleOrDefault<T>();

        #endregion

        #region ToArray

        /// <summary>
        /// Создает список объектов
        /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] ToArray<T>() => _con.Send(this).ToArray<T>();

        public T[] ToArray<T>(Func<MikroTikResponseFrame, T> selector) => _con.Send(this).ToArray<T>(selector);

        /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
        public T[] ToArray<T>(T anonymousObject) => _con.Send(this).ToArray<T>(anonymousObject);

        #endregion

        #region ToList

        /// <summary>
        /// Создает список объектов
        /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> ToList<T>() => _con.Send(this).ToList<T>();

        public List<T> ToList<T>(Func<MikroTikResponseFrame, T> selector) => _con.Send(this).ToList<T>(selector);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
        /// <returns></returns>
        public List<T> ToList<T>(T anonymousObject) => _con.Send(this).ToList<T>(anonymousObject);

        #endregion

        public IAsyncResponse ToAsync()
        {
            return this;
        }

        #region IAsyncResponse

        /// <summary>
        /// Отправляет команду и возвращает ответ сервера.
        /// </summary>
        /// <exception cref="MikroTikTrapException"/>
        /// <exception cref="MikroTikFatalException"/>
        /// <exception cref="MikroTikDisconnectException"/>
        Task<MikroTikResponse> IAsyncResponse.SendAsync() => _con.SendAsync(this);

        async Task<string> IAsyncResponse.Scalar()
        {
            var result = await _con.SendAsync(this).ConfigureAwait(false);
            return result.Scalar();
        }

        async Task<T> IAsyncResponse.Scalar<T>()
        {
            var result = await _con.SendAsync(this).ConfigureAwait(false);
            return result.Scalar<T>();
        }

        async Task<string> IAsyncResponse.Scalar(string columnName)
        {
            var result = await _con.SendAsync(this).ConfigureAwait(false);
            return result.Scalar(columnName);
        }

        async Task<T> IAsyncResponse.Scalar<T>(string columnName)
        {
            var result = await _con.SendAsync(this).ConfigureAwait(false);
            return result.Scalar<T>(columnName);
        }

        async Task<List<string>> IAsyncResponse.ScalarList()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ScalarList();
        }

        async Task<List<T>> IAsyncResponse.ScalarList<T>()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ScalarList<T>();
        }

        async Task<List<string>> IAsyncResponse.ScalarList(string columnName)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ScalarList(columnName);
        }

        async Task<List<T>> IAsyncResponse.ScalarList<T>(string columnName)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ScalarList<T>(columnName);
        }

        async Task<string> IAsyncResponse.ScalarOrDefault()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ScalarOrDefault();
        }

        async Task<T> IAsyncResponse.ScalarOrDefault<T>()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ScalarOrDefault<T>();
        }

        async Task<string> IAsyncResponse.ScalarOrDefault(string columnName)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ScalarOrDefault(columnName);
        }

        async Task<T> IAsyncResponse.ScalarOrDefault<T>(string columnName)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ScalarOrDefault<T>(columnName);
        }

        async Task<MikroTikResponseFrame> IAsyncResponse.Single()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.Single();
        }

        async Task<T> IAsyncResponse.Single<T>(Func<MikroTikResponseFrame, T> selector)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.Single<T>(selector);
        }

        async Task<T> IAsyncResponse.Single<T>()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.Single<T>();
        }

        async Task<MikroTikResponseFrame> IAsyncResponse.SingleOrDefault()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.SingleOrDefault();
        }

        async Task<T> IAsyncResponse.SingleOrDefault<T>(Func<MikroTikResponseFrame, T> selector)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.SingleOrDefault(selector);
        }

        async Task<T> IAsyncResponse.SingleOrDefault<T>()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.SingleOrDefault<T>();
        }

        async Task<T[]> IAsyncResponse.ToArray<T>()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ToArray<T>();
        }

        async Task<T[]> IAsyncResponse.ToArray<T>(Func<MikroTikResponseFrame, T> selector)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ToArray<T>(selector);
        }

        async Task<T[]> IAsyncResponse.ToArray<T>(T anonymousObject)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ToArray<T>(anonymousObject);
        }

        async Task<List<T>> IAsyncResponse.ToList<T>()
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ToList<T>();
        }

        async Task<List<T>> IAsyncResponse.ToList<T>(Func<MikroTikResponseFrame, T> selector)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ToList<T>(selector);
        }

        async Task<List<T>> IAsyncResponse.ToList<T>(T anonymousObject)
        {
            var response = await _con.SendAsync(this).ConfigureAwait(false);
            return response.ToList<T>(anonymousObject);
        }

        #endregion
    }
}
