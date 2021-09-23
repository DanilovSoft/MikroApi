using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Security;

namespace DanilovSoft.MikroApi
{
    public sealed class MikroTikConnection : IDisposable
    {
        private const RouterOsVersion DefaultOsVersion = RouterOsVersion.PostVersion6Dot43;
        private const int DefaultReadWriteTimeout = 30000;
        public const int DefaultPort = 8728;
        public const int DefaultSslPort = 8729;
        public const int ConnectTimeoutMs = 10000;
        
        public static Encoding DefaultEncoding = Encoding.UTF8;
        public static TimeSpan DefaultPingInterval = TimeSpan.FromSeconds(30);

        internal readonly Encoding _encoding;
        private TimeSpan ConnectTimeout => TimeSpan.FromMilliseconds(ConnectTimeoutMs);
        private MikroTikSocket? _socket;
        private bool _disposed;
        private int _tagIndex;
        private int _receiveTimeout = DefaultReadWriteTimeout;
        private int _sendTimeout = DefaultReadWriteTimeout;

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

        public bool Connected { get; private set; }
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
        /// Потокобезопасно создает уникальный тег.
        /// </summary>
        internal string CreateUniqueTag()
        {
            // Создать уникальный tag.
            ushort intTag = unchecked((ushort)Interlocked.Increment(ref _tagIndex));
            return intTag.ToString();
        }

        #region Connect

        public void Connect(string hostname, int port, string login, string password)
        {
            Connect(hostname, port, login, password, DefaultOsVersion);
        }

        public void Connect(string hostname, int port, string login, string password, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43)
        {
            _socket = Connect(hostname, port, ssl: false);
            Login(login, password, version);
        }

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        public void ConnectSsl(string hostname, int port, string login, string password)
        {
            ConnectSsl(hostname, port, login, password, DefaultOsVersion);
        }

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        public void ConnectSsl(string hostname, int port, string login, string password, RouterOsVersion version)
        {
            _socket = Connect(hostname, port, ssl: true);
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

            return Socket.SendAndGetResponse(command);
        }

        /// <summary>
        /// Отправляет команду и возвращает ответ сервера.
        /// </summary>
        public Task<MikroTikResponse> SendAsync(MikroTikCommand command)
        {
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
            command.ThrowIfCompleted();

            // Добавить в словарь.
            MikroTikResponseListener listener = Socket.AddListener();

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
            command.ThrowIfCompleted();

            // Добавить в словарь.
            MikroTikResponseListener listener = Socket.AddListener();

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

        private MikroTikSocket Connect(string hostname, int port, bool ssl)
        {
            var tcp = new TcpClient();

            try
            {
                tcp.Connect(hostname, port);
            }
            catch
            {
                tcp.Dispose();
                throw;
            }

            NetworkStream nstream = tcp.GetStream();

            if (ssl)
            {
                var sslStream = new SslStream(nstream, leaveInnerStreamOpen: false);
                try
                {
                    sslStream.AuthenticateAsClient(hostname);
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

        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        private async Task<MikroTikSocket> ConnectAsync(string hostname, int port, bool ssl, CancellationToken cancellationToken)
        {
            var tcp = new TcpClient();
            var wrapper = new CancellationTokenHelper(tcp, ConnectTimeout, cancellationToken);

            try
            {
                await wrapper.WrapAsync(tcp.ConnectAsync(hostname, port)).ConfigureAwait(false);
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

            NetworkStream nstream = tcp.GetStream();

            if (ssl)
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
        private void CheckConnected()
        {
            if (!Connected)
            {
                throw new MikroTikConnectionException("You are not connected");
            }
        }

        /// <exception cref="MikroTikConnectionException"/>
        private void CheckLoggedIn()
        {
            if (Connected)
            {
                throw new MikroTikConnectionException("You are already connected");
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
            CheckLoggedIn();

            var command = new MikroTikCommand("/login");
            MikroTikResponse resp = _socket.SendAndGetResponse(command);

            string hash = resp[0]["ret"];
            string response = EncodePassword(password, hash);

            var secondCommand = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("response", response);

            _socket.SendAndGetResponse(secondCommand);
            Connected = true;
        }

        private void LoginPlain(string login, string password)
        {
            CheckLoggedIn();

            var command = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("password", password);

            _socket.SendAndGetResponse(command);
            Connected = true;
        }

        private async Task LoginPlainAsync(string login, string password)
        {
            CheckLoggedIn();

            var command = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("password", password);

            MikroTikResponse resp = await _socket.SendAndGetResponseAsync(command).ConfigureAwait(false);
            Connected = true;
        }

        /// <summary>
        /// Использует MD5.
        /// </summary>
        private async Task LoginAsync(string login, string password)
        {
            CheckLoggedIn();

            var command = new MikroTikCommand("/login");
            MikroTikResponse resp = await _socket.SendAndGetResponseAsync(command).ConfigureAwait(false);

            string hash = resp[0]["ret"];
            string response = EncodePassword(password, hash);

            var secondCommand = new MikroTikCommand("/login")
                .Attribute("name", login)
                .Attribute("response", response);

            await _socket.SendAndGetResponseAsync(secondCommand).ConfigureAwait(false);
            Connected = true;
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
    }
}
