using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    public interface IAsyncResponse
    {
        /// <summary>
        /// Отправляет команду и возвращает ответ сервера.
        /// </summary>
        /// <exception cref="MikroTikTrapException"/>
        /// <exception cref="MikroTikFatalException"/>
        /// <exception cref="MikroTikDisconnectException"/>
        Task<MikroTikResponse> SendAsync();
        /// <exception cref="InvalidOperationException"/>

        #region Scalar

        /// <exception cref="InvalidOperationException"/>
        Task<string> Scalar();

        /// <exception cref="InvalidOperationException"/>
        Task<T> Scalar<T>();

        /// <exception cref="InvalidOperationException"/>
        Task<string> Scalar(string columnName);

        /// <exception cref="InvalidOperationException"/>
        Task<T> Scalar<T>(string columnName);

        #endregion

        #region ScalarList

        /// <exception cref="InvalidOperationException"/>
        Task<List<string>> ScalarList();

        /// <exception cref="InvalidOperationException"/>
        Task<List<T>> ScalarList<T>();

        Task<List<string>> ScalarList(string columnName);

        Task<List<T>> ScalarList<T>(string columnName);

        #endregion

        #region ScalarOrDefault

        /// <summary>
        /// Возвращает <see langword="null"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        Task<string> ScalarOrDefault();

        /// <summary>
        /// Возвращает <see langword="default"/> если нет ни одной строки.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        Task<T> ScalarOrDefault<T>();

        /// <summary>
        /// Возвращает <see langword="null"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        Task<string> ScalarOrDefault(string columnName);

        /// <summary>
        /// Возвращает <see langword="default"/> если нет ни одной строки.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        Task<T> ScalarOrDefault<T>(string columnName);

        #endregion

        #region Single

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        Task<MikroTikResponseFrame> Single();

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        Task<T> Single<T>(Func<MikroTikResponseFrame, T> selector);

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        Task<T> Single<T>();

        #endregion

        #region SingleOrDefault

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        Task<MikroTikResponseFrame> SingleOrDefault();

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        Task<T> SingleOrDefault<T>(Func<MikroTikResponseFrame, T> selector);

        /// <summary>
        /// Когда результатом является одна строка.
        /// </summary>
        Task<T> SingleOrDefault<T>();

        #endregion

        #region ToArray

        /// <summary>
        /// Создает список объектов
        /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<T[]> ToArray<T>();

        Task<T[]> ToArray<T>(Func<MikroTikResponseFrame, T> selector);

        /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
        Task<T[]> ToArray<T>(T anonymousObject);

        #endregion

        #region ToList

        /// <summary>
        /// Создает список объектов
        /// члены которого должны использовать атрибут <see cref="MikroTikPropertyAttribute"/> для привязки данных.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<List<T>> ToList<T>();

        Task<List<T>> ToList<T>(Func<MikroTikResponseFrame, T> selector);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="anonymousObject">Анонимный объект тип которого используется для создания результата функции.</param>
        /// <returns></returns>
        Task<List<T>> ToList<T>(T anonymousObject);

        #endregion
    }
}
