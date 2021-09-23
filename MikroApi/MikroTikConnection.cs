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

namespace DanilovSoft.MikroApi
{
    public sealed class MikroTikConnection : IDisposable
    {
        private const RouterOsVersion DefaultOsVersion = RouterOsVersion.PostVersion6Dot43;
        private const int DefaultReadWriteTimeout = 30000;
        public const int DefaultPort = 8728;
        public const int DefaultSslPort = 8729;
        public const int ConnectTimeoutMs = 10000;
        
        public static Encoding DefaultEncoding { get; } = Encoding.UTF8;
        public static TimeSpan DefaultPingInterval { get; } = TimeSpan.FromSeconds(30);

        internal readonly Encoding _encoding;
        private TimeSpan ConnectTimeout => TimeSpan.FromMilliseconds(ConnectTimeoutMs);
        private MikroTikSocket? _socket;
        private bool _disposed;
        private int _tagIndex;
        private int _receiveTimeout = DefaultReadWriteTimeout;
        private int _sendTimeout = DefaultReadWriteTimeout;
        private bool _authorized;

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
                _socket?.Dispose();
                _socket = null;
            }
        }

        public bool Connected => _authorized;
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

        /// <exception cref="InvalidOperationException"/>
        private MikroTikSocket Socket
        {
            get
            {
                CheckConnected();
                return _socket;
            }
        }

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

        #region Connect

        /// <exception cref="ObjectDisposedException"/>
        public void Connect(string hostname, int port, string login, string password)
        {
            Connect(hostname, port, login, password, DefaultOsVersion);
        }

        /// <exception cref="ObjectDisposedException"/>
        public void Connect(string hostname, int port, string login, string password, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43)
        {
            CheckDisposed();

            _socket = Connect(hostname, port, useSsl: false);
            Login(login, password, version);
        }

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="ObjectDisposedException"/>
        public void ConnectSsl(string hostname, int port, string login, string password)
        {
            CheckDisposed();

            ConnectSsl(hostname, port, login, password, DefaultOsVersion);
        }

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="ObjectDisposedException"/>
        public void ConnectSsl(string hostname, int port, string login, string password, RouterOsVersion version)
        {
            CheckDisposed();

            _socket = Connect(hostname, port, useSsl: true);
            Login(login, password, version);
        }

        /// <exception cref="TimeoutException"/>
        public Task ConnectAsync(string hostname, int port, string login, string password)
        {
            return ConnectAsync(hostname, port, login, password, DefaultOsVersion, CancellationToken.None);
        }

        /// <exception cref="TimeoutException"/>
        public Task ConnectAsync(string hostname, int port, string login, string password, RouterOsVersion version)
        {
            return ConnectAsync(hostname, port, login, password, version, CancellationToken.None);
        }

        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        public async Task ConnectAsync(string hostname, int port, string login, string password, RouterOsVersion version, CancellationToken cancellationToken)
        {
            _socket = await ConnectAsync(hostname, port, false, cancellationToken).ConfigureAwait(false);
            await LoginAsync(login, password, version).ConfigureAwait(false);
        }

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        public Task ConnectSslAsync(string hostname, int port, string login, string password)
        {
            return ConnectSslAsync(hostname, port, login, password, DefaultOsVersion, CancellationToken.None);
        }

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        public Task ConnectSslAsync(string hostname, int port, string login, string password, RouterOsVersion version)
        {
            return ConnectSslAsync(hostname, port, login, password, version, CancellationToken.None);
        }

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        public async Task ConnectSslAsync(string hostname, int port, string login, string password, RouterOsVersion version, CancellationToken cancellationToken)
        {
            _socket = await ConnectAsync(hostname, port, true, cancellationToken).ConfigureAwait(false);
            await LoginAsync(login, password, version).ConfigureAwait(false);
        }

        #endregion

        #region Send

        /// <summary>
        /// Отправляет команду и возвращает ответ сервера.
        /// </summary>
        /// <exception cref="MikroTikTrapException"/>
        public MikroTikResponse Send(MikroTikCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            CheckConnected();

            return _socket.SendAndGetResponse(command);
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

            return Socket.SendAndGetResponseAsync(command);
        }

        #endregion

        #region Listen

        /// <summary>
        /// Отправляет команду помечая её тегом.
        /// Команда будет выполняться пока не будет прервана с помощью Cancel.
        /// </summary>
        /// <exception cref="MikroTikTrapException"/>
        public MikroTikResponseListener Listen(MikroTikCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.ThrowIfCompleted();

            // Добавить в словарь.
            var listener = Socket.AddListener();

            command.SetTag(listener._tag);

            // Синхронная отправка команды в сокет без получения результата.
            // Последовательность результатов будет делегироваться в Listener.
            Socket.Send(command);

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

            command.ThrowIfCompleted();

            // Добавить в словарь.
            var listener = Socket.AddListener();

            command.SetTag(listener._tag);

            // Асинхронная отправка запроса в сокет.
            await Socket.SendAsync(command).ConfigureAwait(false);

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
            return Socket.CancelListenersAsync(wait: true);
        }

        /// <summary>
        /// Отправляет запрос на отмену всех выполняющихся задач.
        /// </summary>
        /// <param name="wait">True если нужно дождаться подтверждения об успешной отмене. Значение по умолчанию True</param>
        public void CancelListeners(bool wait)
        {
            Socket.CancelListeners(wait);
        }

        /// <summary>
        /// Отправляет запрос на отмену всех выполняющихся задач.
        /// </summary>
        /// <param name="wait">True если нужно дождаться подтверждения об успешной отмене. Значение по умолчанию True</param>
        public Task CancelListenersAsync(bool wait)
        {
            return Socket.CancelListenersAsync(wait);
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
            return Socket.Quit(millisecondsTimeout);
        }

        /// <summary>
        /// Сообщает серверу что выполняется разъединение. Не бросает исключения.
        /// </summary>
        /// <param name="millisecondsTimeout">Позволяет подождать подтверждение от сервера что-бы лишний раз не происходило исключение в потоке читающем из сокета.</param>
        public Task<bool> QuitAsync(int millisecondsTimeout)
        {
            return Socket.QuitAsync(millisecondsTimeout);
        }

        #region Non Public

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

        private MikroTikSocket Connect(string hostname, int port, bool useSsl)
        {
            var tcp = ConnectTcp(hostname, port);
            var tcpStream = tcp.GetStream();
            try
            {
                Stream finalStream = useSsl 
                    ? AuthenticateSsl(tcpStream, hostname) 
                    : tcpStream;

                NullableHelper.SetNull(ref tcpStream);
                return new MikroTikSocket(this, NullableHelper.SetNull(ref tcp), finalStream);
            }
            finally
            {
                tcpStream?.Dispose();
                tcp?.Dispose();
            }
        }

        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        private async Task<MikroTikSocket> ConnectAsync(string hostname, int port, bool useSsl, CancellationToken cancellationToken)
        {
            var tcp = new TcpClient();
            var wrapper = new CancellationTokenHelper(tcp, ConnectTimeout, cancellationToken);
            try
            {
                await wrapper.WrapAsync(tcp.ConnectAsync(hostname, port, cancellationToken)).ConfigureAwait(false);
            }
            catch
            {
                // Закрыть сокет если это еще не сделал враппер.
                if (!wrapper.IsDisposed)
                {
                    tcp.Dispose();
                }

                throw;
            }

            var nstream = tcp.GetStream();

            if (useSsl)
            {
                var sslStream = new SslStream(nstream, leaveInnerStreamOpen: false);
                try
                {
                    await sslStream.AuthenticateAsClientAsync(hostname).ConfigureAwait(false);
                }
                catch
                {
                    sslStream.Dispose();
                    tcp.Dispose();
                    throw;
                }
                return new MikroTikSocket(this, tcp, sslStream);
            }
            else
            {
                return new MikroTikSocket(this, tcp, nstream);
            }
        }

        /// <exception cref="MikroTikConnectionException"/>
        [MemberNotNull(nameof(_socket))]
        private void CheckConnected()
        {
            if (!Connected)
            {
                ThrowHelper.ThrowNotConnected();
            }
        }

        /// <exception cref="MikroTikConnectionException"/>
        //[MemberNotNull(nameof(_socket))]
        private void CheckAlreadyAuthorized()
        {
            if (_authorized)
            {
                ThrowHelper.ThrowAlreadyConnected();
            }
        }

        #region Login

        private Task LoginAsync(string login, string password, RouterOsVersion version)
        {
            if (version == RouterOsVersion.PostVersion6Dot43)
            {
                return LoginPlainAsync(login, password);
            }
            else
            {
                return LoginAsync(login, password);
            }
        }

        private void Login(string login, string password, RouterOsVersion version)
        {
            if (version == RouterOsVersion.PostVersion6Dot43)
            {
                LoginPlain(login, password);
            }
            else
            {
                Login(login, password);
            }
        }

        /// <summary>
        /// Использует MD5.
        /// </summary>
        private void Login(string login, string password)
        {
            CheckAlreadyAuthorized();

            var command = new MikroTikCommand("/login");
            var resp = _socket.SendAndGetResponse(command);

            string hash = resp[0]["ret"];
            string response = EncodePassword(password, hash);

            var secondCommand = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("response", response);

            _socket.SendAndGetResponse(secondCommand);
            _authorized = true;
        }

        private void LoginPlain(string login, string password)
        {
            CheckAlreadyAuthorized();

            var command = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("password", password);

            _socket.SendAndGetResponse(command);
            _authorized = true;
        }

        private async Task LoginPlainAsync(string login, string password)
        {
            CheckAlreadyAuthorized();

            var command = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("password", password);

            MikroTikResponse resp = await _socket.SendAndGetResponseAsync(command).ConfigureAwait(false);
            _authorized = true;
        }

        /// <summary>
        /// Использует MD5.
        /// </summary>
        private async Task LoginAsync(string login, string password)
        {
            CheckAlreadyAuthorized();

            var command = new MikroTikCommand("/login");
            var resp = await _socket.SendAndGetResponseAsync(command).ConfigureAwait(false);

            string hash = resp[0]["ret"];
            string response = EncodePassword(password, hash);

            var secondCommand = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("response", response);

            await _socket.SendAndGetResponseAsync(secondCommand).ConfigureAwait(false);
            _authorized = true;
        }

        #endregion

        /// <summary>
        /// Возвращает HEX строку длиной 34 символа.
        /// </summary>
        /// <param name="password">Пароль пользователя.</param>
        /// <param name="hash">Хеш от микротика.</param>
        private string EncodePassword(string password, string hash)
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

            byte[] pass = _encoding.GetBytes(password);
            byte[] buf = new byte[pass.Length + bHash.Length + 1];

            Array.Copy(pass, 0, buf, 1, pass.Length);
            Array.Copy(bHash, 0, buf, pass.Length + 1, bHash.Length);

            using (var md5 = MD5.Create())
            {
                byte[] cHash = md5.ComputeHash(buf);

                var sb = new StringBuilder(34, 34);
                sb.Append("00");
                for (int i = 0; i < 16; i++)
                {
                    sb.Append(cHash[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

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
