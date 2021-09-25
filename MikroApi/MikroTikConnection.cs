using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Security;
using DanilovSoft.MikroApi.Helpers;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;

namespace DanilovSoft.MikroApi
{
    public sealed class MikroTikConnection : IMikroTikConnection, IDisposable
    {
        public const int DefaultApiPort = 8728;
        public const int DefaultApiSslPort = 8729;
        public static Encoding DefaultEncoding { get; set; } = Encoding.UTF8;
        public static TimeSpan DefaultPingInterval { get; set; } = TimeSpan.FromSeconds(30);

        private const RouterOsVersion DefaultOsVersion = RouterOsVersion.PostVersion6Dot43;
        private const int DefaultReadWriteTimeout = 30000;
        internal readonly Encoding _encoding;
        //private TimeSpan ConnectTimeout => TimeSpan.FromMilliseconds(ConnectTimeoutMs);
        private MtOpenConnection? _authorizedSocket;
        private bool _disposed;
        private int _tagIndex;
        private int _receiveTimeout = DefaultReadWriteTimeout;
        private int _sendTimeout = DefaultReadWriteTimeout;
        //private bool _authorized;

        // ctor
        public MikroTikConnection() : this(DefaultEncoding)
        {
            // Этот конструктор лучше оставить пустым.
        }

        // ctor
        public MikroTikConnection(Encoding encoding)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _authorizedSocket?.Dispose();
                _authorizedSocket = null;
            }
        }

        public bool Connected => _authorizedSocket != null;
        public int ReceiveTimeout
        {
            get => _receiveTimeout;
            set
            {
                if (Connected)
                {
                    throw new InvalidOperationException("Can't change receive timeout after connection");
                }

                _receiveTimeout = value;
            }
        }
        public int SendTimeout
        {
            get => _sendTimeout;
            set
            {
                if (Connected)
                {
                    throw new InvalidOperationException("Can't change send timeout after connection");
                }

                _sendTimeout = value;
            }
        }

        #region Connect
        
        public void Connect(string login, string password, string hostname, int port = DefaultApiPort)
        {
            Connect(login, password, hostname, port, DefaultOsVersion);
        }

        public void Connect(string login, string password, string hostname, int port, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43)
        {
            if (hostname is null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            CheckDisposed();
            CheckNotAuthorized();

            var socket = ConnectCore(hostname, port, useSsl: false);
            try
            {
                Login(socket, login, password, version);
                _authorizedSocket = NullableHelper.SetNull(ref socket);
            }
            finally
            {
                socket?.Dispose();
            }
        }

        public void ConnectSsl(string login, string password, string hostname, int port = DefaultApiSslPort)
        {
            ConnectSsl(login, password, hostname, port, DefaultOsVersion);
        }

        public void ConnectSsl(string login, string password, string hostname, int port, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43)
        {
            if (hostname is null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            CheckDisposed();
            CheckNotAuthorized();

            var socket = ConnectCore(hostname, port, useSsl: true);
            try
            {
                Login(socket, login, password, version);
                _authorizedSocket = NullableHelper.SetNull(ref socket);
            }
            finally
            {
                socket?.Dispose();
            }
        }

        public Task ConnectAsync(string login, string password, string hostname, int port = DefaultApiPort, CancellationToken cancellationToken = default)
        {
            return ConnectAsync(login, password, hostname, port, DefaultOsVersion, cancellationToken);
        }

        public async Task ConnectAsync(string login, string password, string hostname, int port,
                                       RouterOsVersion version = RouterOsVersion.PostVersion6Dot43,
                                       CancellationToken cancellationToken = default)
        {
            if (hostname is null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            CheckDisposed();
            CheckNotAuthorized();

            var socket = await ConnectAsyncCore(hostname, port, false, cancellationToken).ConfigureAwait(false);
            try
            {
                await LoginAsync(socket, login, password, version).ConfigureAwait(false);
                _authorizedSocket = NullableHelper.SetNull(ref socket);
            }
            finally
            {
                socket?.Dispose();
            }
        }

        public Task ConnectSslAsync(string login, string password, string hostname, int port = DefaultApiSslPort, CancellationToken cancellationToken = default)
        {
            return ConnectSslAsync(login, password, hostname, port, DefaultOsVersion, cancellationToken);
        }

        public async Task ConnectSslAsync(string login, string password, string hostname, int port, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43,
                                          CancellationToken cancellationToken = default)
        {
            if (hostname is null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            CheckDisposed();
            CheckNotAuthorized();

            var socket = await ConnectAsyncCore(hostname, port, true, cancellationToken).ConfigureAwait(false);
            try
            {
                await LoginAsync(socket, login, password, version).ConfigureAwait(false);
                _authorizedSocket = NullableHelper.SetNull(ref socket);
            }
            finally
            {
                socket?.Dispose();
            }
        }

        #endregion

        #region Send

        /// <summary>
        /// Отправляет команду и возвращает ответ сервера.
        /// </summary>
        /// <exception cref="MikroApiTrapException"/>
        public MikroTikResponse Send(MikroTikCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var connection = CheckAuthorized();

            return connection.SendAndGetResponse(command);
        }

        /// <summary>
        /// Отправляет команду и возвращает ответ сервера.
        /// </summary>
        public Task<MikroTikResponse> SendAsync(MikroTikCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var connection = CheckAuthorized();

            return connection.SendAndGetResponseAsync(command);
        }

        #endregion

        #region Listen

        /// <summary>
        /// Отправляет команду помечая её тегом.
        /// Команда будет выполняться пока не будет прервана с помощью Cancel.
        /// </summary>
        /// <exception cref="MikroApiTrapException"/>
        public MikroTikResponseListener Listen(MikroTikCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.CheckCompleted();

            var connection = CheckAuthorized();

            // Добавить в словарь.
            var listener = connection.AddListener();

            command.SetTag(listener._tag);

            // Синхронная отправка команды в сокет без получения результата.
            // Последовательность результатов будет делегироваться в Listener.
            connection.Send(command);

            command.Completed();

            return listener;
        }

        /// <summary>
        /// Отправляет команду помечая её тегом.
        /// Команда будет выполняться пока не будет прервана с помощью Cancel.
        /// </summary>
        public async Task<MikroTikResponseListener> ListenAsync(MikroTikCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.CheckCompleted();

            var connection = CheckAuthorized();

            // Добавить в словарь.
            var listener = connection.AddListener();

            command.SetTag(listener._tag);

            // Асинхронная отправка запроса в сокет.
            await connection.SendAsync(command).ConfigureAwait(false);

            command.Completed();

            return listener;
        }

        #endregion

        /// <summary>
        /// Отправляет запрос на отмену всех выполняющихся задач и ожидает подтверждения об упешной отмене.
        /// </summary>
        public void CancelListeners()
        {
            CancelListeners(wait: true);
        }

        /// <summary>
        /// Отправляет запрос на отмену всех выполняющихся задач и ожидает подтверждения об упешной отмене.
        /// </summary>
        public Task CancelListenersAsync()
        {
            var connection = CheckAuthorized();

            return connection.CancelListenersAsync(wait: true);
        }

        /// <summary>
        /// Отправляет запрос на отмену всех выполняющихся задач.
        /// </summary>
        /// <param name="wait">True если нужно дождаться подтверждения об успешной отмене. Значение по умолчанию True</param>
        public void CancelListeners(bool wait)
        {
            CheckAuthorized();

            _authorizedSocket.CancelListeners(wait);
        }

        /// <summary>
        /// Отправляет запрос на отмену всех выполняющихся задач.
        /// </summary>
        /// <param name="wait">True если нужно дождаться подтверждения об успешной отмене. Значение по умолчанию True</param>
        public Task CancelListenersAsync(bool wait)
        {
            CheckAuthorized();

            return _authorizedSocket.CancelListenersAsync(wait);
        }

        /// <summary>
        /// Подготавливает команду для отправки.
        /// </summary>
        /// <param name="command">Начальный текст команды</param>
        public MikroTikFlowCommand Command(string command)
        {
            return new MikroTikFlowCommand(command, this);
        }

        /// <summary>
        /// Сообщает серверу что выполняется разъединение. Не бросает исключения.
        /// </summary>
        /// <param name="millisecondsTimeout">Позволяет подождать подтверждение от сервера что-бы лишний раз не происходило исключение в потоке читающем из сокета.</param>
        public bool Quit(int millisecondsTimeout)
        {
            CheckAuthorized();

            return _authorizedSocket.Quit(millisecondsTimeout);
        }

        /// <summary>
        /// Сообщает серверу что выполняется разъединение. Не бросает исключения.
        /// </summary>
        /// <param name="millisecondsTimeout">Позволяет подождать подтверждение от сервера что-бы лишний раз не происходило исключение в потоке читающем из сокета.</param>
        public Task<bool> QuitAsync(int millisecondsTimeout)
        {
            CheckAuthorized();

            return _authorizedSocket.QuitAsync(millisecondsTimeout);
        }

        #region Non Public

        /// <summary>
        /// Потокобезопасно создает следующий уникальный тег.
        /// </summary>
        /// <remarks>От 0 до 65535.</remarks>
        internal string CreateUniqueTag()
        {
            // Создать уникальный tag.
            ushort intTag = unchecked((ushort)Interlocked.Increment(ref _tagIndex));
            return intTag.ToString(CultureInfo.InvariantCulture);
        }

        private static TcpClient ConnectTcp(string hostname, int port)
        {
            var tcp = new TcpClient();
            try
            {
                tcp.Connect(hostname, port);
                return NullableHelper.SetNull(ref tcp);
            }
            finally
            {
                tcp?.Dispose();
            }
        }

        private static async Task<TcpClient> ConnectTcpAsync(string hostname, int port, CancellationToken cancellationToken)
        {
            var tcp = new TcpClient();
            try
            {
                await tcp.ConnectAsync(hostname, port, cancellationToken).ConfigureAwait(false);
                return NullableHelper.SetNull(ref tcp);
            }
            finally
            {
                tcp?.Dispose();
            }
        }

        private static SslStream AuthenticateSsl(Stream stream, string hostname)
        {
            var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
            try
            {
                sslStream.AuthenticateAsClient(hostname);
                return NullableHelper.SetNull(ref sslStream);
            }
            finally
            {
                sslStream?.Dispose();
            }
        }

        private static async Task<SslStream> AuthenticateSslAsync(Stream stream, string hostname, CancellationToken cancellationToken)
        {
            var options = new SslClientAuthenticationOptions { TargetHost = hostname };
            var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
            try
            {
                await sslStream.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
                return NullableHelper.SetNull(ref sslStream);
            }
            finally
            {
                sslStream?.Dispose();
            }
        }

        private static async Task LoginPlainAsync(MtOpenConnection socket, string login, string password)
        {
            //CheckAlreadyAuthorized();

            var command = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("password", password);

            _ = await socket.SendAndGetResponseAsync(command).ConfigureAwait(false);
        }

        /// <summary>
        /// Возвращает HEX строку длиной 34 символа.
        /// </summary>
        /// <param name="password">Пароль пользователя.</param>
        /// <param name="hash">Хеш от микротика.</param>
        private static string EncodePassword(string password, string hash, Encoding encoding)
        {
            if (hash.Length != 32)
            {
                throw new ArgumentOutOfRangeException(nameof(hash));
            }

            byte[] bHash = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                bHash[i] = Convert.ToByte(hash.Substring(i * 2, 2), 16);
            }

            byte[] pass = encoding.GetBytes(password);
            byte[] buf = new byte[pass.Length + bHash.Length + 1];

            Array.Copy(pass, 0, buf, 1, pass.Length);
            Array.Copy(bHash, 0, buf, pass.Length + 1, bHash.Length);

            Span<byte> cHash = stackalloc byte[16];
            HashChallengeResponse(buf, cHash);
            
            var sb = new StringBuilder(34, 34);
            sb.Append("00");
            for (int i = 0; i < 16; i++)
            {
                sb.Append(cHash[i].ToString("X2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        [SuppressMessage("Security", "CA5351:Не используйте взломанные алгоритмы шифрования", Justification = "Для обратной совместимости")]
        private static void HashChallengeResponse(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            MD5.HashData(source, destination);
        }

        private string EncodePassword(string password, string hash)
        {
            return EncodePassword(password, hash, _encoding);
        }

        private MtOpenConnection ConnectCore(string hostname, int port, bool useSsl)
        {
            var tcp = ConnectTcp(hostname, port);
            var tcpStream = tcp.GetStream();
            try
            {
                Stream finalStream = useSsl
                    ? AuthenticateSsl(tcpStream, hostname)
                    : tcpStream;

                NullableHelper.SetNull(ref tcpStream);
                return new MtOpenConnection(this, NullableHelper.SetNull(ref tcp), finalStream);
            }
            finally
            {
                tcpStream?.Dispose();
                tcp?.Dispose();
            }
        }

        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        private async Task<MtOpenConnection> ConnectAsyncCore(string hostname, int port, bool useSsl, CancellationToken cancellationToken)
        {
            var tcp = await ConnectTcpAsync(hostname, port, cancellationToken).ConfigureAwait(false);
            var tcpStream = tcp.GetStream();
            try
            {
                Stream finalStream = useSsl
                    ? await AuthenticateSslAsync(tcpStream, hostname, cancellationToken).ConfigureAwait(false)
                    : tcpStream;

                NullableHelper.SetNull(ref tcpStream);
                return new MtOpenConnection(this, NullableHelper.SetNull(ref tcp), finalStream);
            }
            finally
            {
                tcpStream?.Dispose();
                tcp?.Dispose();
            }
        }

        /// <exception cref="MikroApiConnectionException"/>
        [MemberNotNull(nameof(_authorizedSocket))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MtOpenConnection CheckAuthorized()
        {
            if (_authorizedSocket == null)
            {
                ThrowHelper.ThrowNotConnected();
            }
            return _authorizedSocket;
        }

        /// <exception cref="MikroApiConnectionException"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckNotAuthorized()
        {
            if (_authorizedSocket == null)
            {
                return;
            }
            ThrowHelper.ThrowAlreadyConnected();
        }

        #region Login

        private Task LoginAsync(MtOpenConnection socket, string login, string password, RouterOsVersion version)
        {
            if (version == RouterOsVersion.PostVersion6Dot43)
            {
                return LoginPlainAsync(socket, login, password);
            }
            else
            {
                return LoginAsync(socket, login, password);
            }
        }

        private void Login(MtOpenConnection socket, string login, string password, RouterOsVersion version)
        {
            if (version == RouterOsVersion.PostVersion6Dot43)
            {
                LoginPlain(socket, login, password);
            }
            else
            {
                Login(socket, login, password);
            }
        }

        /// <summary>
        /// Использует MD5.
        /// </summary>
        private void Login(MtOpenConnection socket, string login, string password)
        {
            var command = new MikroTikCommand("/login");
            var resp = socket.SendAndGetResponse(command);

            string hash = resp[0]["ret"];
            string response = EncodePassword(password, hash);

            var secondCommand = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("response", response);

            socket.SendAndGetResponse(secondCommand);
        }

        private void LoginPlain(MtOpenConnection socket, string login, string password)
        {
            CheckNotAuthorized();

            var command = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("password", password);

            socket.SendAndGetResponse(command);
        }

        /// <summary>
        /// Использует MD5.
        /// </summary>
        private async Task LoginAsync(MtOpenConnection socket, string login, string password)
        {
            var command = new MikroTikCommand("/login");
            var resp = await socket.SendAndGetResponseAsync(command).ConfigureAwait(false);

            string hash = resp[0]["ret"];
            string response = EncodePassword(password, hash);

            var secondCommand = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("response", response);

            _ = await socket.SendAndGetResponseAsync(secondCommand).ConfigureAwait(false);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (!_disposed)
            {
                return;
            }
            ThrowHelper.ThrowConnectionDisposed();
        }

        #endregion
    }
}
