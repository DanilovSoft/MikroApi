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

namespace DanilovSoft.MikroApi;

public sealed class MikroTikConnection : IDisposable, IAsyncDisposable
{
    public const int DefaultApiPort = 8728;
    public const int DefaultApiSslPort = 8729;
    public static Encoding DefaultEncoding { get; set; } = Encoding.UTF8;
    private static int _defaultQuitDelay = 2000;
    public static int DefaultQuitDelay
    {
        get => _defaultQuitDelay;
        set
        {
            ArgumentHelper.ValidateTimeout(value);
            _defaultQuitDelay = value;
        }
    }
    private const RouterOsVersion DefaultOsVersion = RouterOsVersion.PostVersion6Dot43;
    internal readonly Encoding _encoding;
    private MtOpenConnection? _openConnection;
    private bool _disposed;
    private int _tagIdSequence;
    private int _receiveTimeout = 30_000;
    private int _sendTimeout = 30_000;
    
    public MikroTikConnection() : this(DefaultEncoding)
    {
        // Этот конструктор лучше оставить пустым.
    }

    public MikroTikConnection(Encoding encoding)
    {
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            NullableHelper.SetNull(ref _openConnection)?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Connected)
        {
            try
            {
                await QuitAsync(DefaultQuitDelay, CancellationToken.None).ConfigureAwait(false);
            }
            catch { }
        }

        Dispose();
    }

    public bool Connected => _openConnection != null;

    public int ReceiveTimeout
    {
        get => _receiveTimeout;
        set
        {
            ArgumentHelper.ValidateTimeout(value);
            CheckNotConnected();
            _receiveTimeout = value;
        }
    }

    public int SendTimeout
    {
        get => _sendTimeout;
        set
        {
            ArgumentHelper.ValidateTimeout(value);
            CheckNotConnected();
            _sendTimeout = value;
        }
    }

    #region Connect

    public void Connect(string login, string password, string host, bool useSsl, int port = DefaultApiPort, 
        CancellationToken cancellationToken = default)
    {
        Connect(login, password, host, useSsl, port, DefaultOsVersion, cancellationToken);
    }

    public void Connect(string login, string password, string host, bool useSsl, int port, RouterOsVersion version, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(login);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(host);
        CheckDisposed();
        CheckNotAuthorized();

        var socket = ConnectCore(host, port, useSsl);
        try
        {
            Login(socket, login, password, version, cancellationToken);
            _openConnection = NullableHelper.SetNull(ref socket);
        }
        finally
        {
            socket?.Dispose();
        }
    }

    public Task ConnectAsync(string login, string password, string hostname, bool useSsl, int port = DefaultApiPort, CancellationToken cancellationToken = default)
    {
        return ConnectAsync(login, password, hostname, useSsl, port, DefaultOsVersion, cancellationToken);
    }

    public async Task ConnectAsync(
        string login,
        string password,
        string host,
        bool useSsl,
        int port,
        RouterOsVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(login);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(host);
        CheckDisposed();
        CheckNotAuthorized();

        var socket = await ConnectAsyncCore(host, port, useSsl, cancellationToken).ConfigureAwait(false);
        try
        {
            await LoginAsync(socket, login, password, version, cancellationToken).ConfigureAwait(false);
            _openConnection = NullableHelper.SetNull(ref socket);
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
    public MikroTikResponse Execute(MikroTikCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var connection = GetOpenConnection();
        return connection.Execute(command, cancellationToken);
    }

    /// <summary>
    /// Отправляет команду и возвращает ответ сервера.
    /// </summary>
    public Task<MikroTikResponse> ExecuteAsync(MikroTikCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var connection = GetOpenConnection();
        return connection.ExecuteAsync(command, cancellationToken);
    }

    #endregion

    #region Listen

    /// <summary>
    /// Отправляет команду помечая её тегом.
    /// Команда будет выполняться пока не будет прервана с помощью Cancel.
    /// </summary>
    /// <exception cref="MikroApiTrapException"/>
    public MikroTikResponseListener Listen(MikroTikCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        command.CheckAndMarkAsUsed();
        var connection = GetOpenConnection();
        var listener = connection.AddListener(); // Добавить в словарь.
        command.SetTag(listener._tag);

        // Синхронная отправка команды в сокет без получения результата.
        // Последовательность результатов будет делегироваться в Listener.
        connection.Send(command, cancellationToken);

        return listener;
    }

    /// <summary>
    /// Отправляет команду помечая её тегом.
    /// Команда будет выполняться пока не будет прервана с помощью Cancel.
    /// </summary>
    public async Task<MikroTikResponseListener> ListenAsync(MikroTikCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        command.CheckAndMarkAsUsed();
        var connection = GetOpenConnection();
        var listener = connection.AddListener(); // Добавить в словарь.
        command.SetTag(listener._tag);

        // Асинхронная отправка запроса в сокет.
        await connection.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return listener;
    }

    #endregion

    /// <summary>
    /// Отправляет запрос на отмену всех выполняющихся задач и ожидает подтверждения об упешной отмене.
    /// </summary>
    public void CancelListeners(CancellationToken cancellationToken = default)
    {
        CancelListeners(waitACK: true, cancellationToken);
    }

    /// <summary>
    /// Отправляет запрос на отмену всех выполняющихся задач и ожидает подтверждения об упешной отмене.
    /// </summary>
    public Task CancelListenersAsync(CancellationToken cancellationToken = default)
    {
        var connection = GetOpenConnection();
        return connection.CancelListenersAsync(waitACK: true, cancellationToken);
    }

    /// <summary>
    /// Отправляет запрос на отмену всех выполняющихся задач.
    /// </summary>
    /// <param name="waitACK">True если нужно дождаться подтверждения об успешной отмене.</param>
    public void CancelListeners(bool waitACK, CancellationToken cancellationToken = default)
    {
        var connection = GetOpenConnection();
        connection.CancelListeners(waitACK, cancellationToken);
    }

    /// <summary>
    /// Отправляет запрос на отмену всех выполняющихся задач.
    /// </summary>
    /// <param name="waitACK">True если нужно дождаться подтверждения об успешной отмене. Значение по умолчанию True</param>
    public Task CancelListenersAsync(bool waitACK, CancellationToken cancellationToken = default)
    {
        var connection = GetOpenConnection();
        return connection.CancelListenersAsync(waitACK, cancellationToken);
    }

    /// <summary>
    /// Подготавливает команду для отправки.
    /// </summary>
    /// <param name="command">Начальный текст команды</param>
    public MikroTikFlowCommand Command(string command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return new MikroTikFlowCommand(command, this);
    }

    /// <summary>
    /// Сообщает серверу что выполняется разъединение. Не бросает исключения.
    /// </summary>
    public bool Quit(CancellationToken cancellationToken = default)
    {
        var connection = GetOpenConnection();
        return connection.Quit(DefaultQuitDelay, cancellationToken);
    }

    /// <summary>
    /// Сообщает серверу что выполняется разъединение. Не бросает исключения.
    /// </summary>
    /// <param name="millisecondsTimeout">Позволяет подождать подтверждение от сервера что-бы лишний раз не происходило исключение в потоке читающем из сокета.</param>
    public bool Quit(int millisecondsTimeout, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ValidateTimeout(millisecondsTimeout);

        var connection = GetOpenConnection();
        return connection.Quit(millisecondsTimeout, cancellationToken);
    }

    /// <summary>
    /// Сообщает серверу что выполняется разъединение. Не бросает исключения.
    /// </summary>
    public Task<bool> QuitAsync(CancellationToken cancellationToken = default)
    {
        var connection = GetOpenConnection();
        return connection.QuitAsync(DefaultQuitDelay, cancellationToken);
    }

    /// <summary>
    /// Сообщает серверу что выполняется разъединение. Не бросает исключения.
    /// </summary>
    /// <param name="millisecondsTimeout">Позволяет подождать подтверждение от сервера что-бы лишний раз не происходило исключение в потоке читающем из сокета.</param>
    public Task<bool> QuitAsync(int millisecondsTimeout, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ValidateTimeout(millisecondsTimeout);

        var connection = GetOpenConnection();
        return connection.QuitAsync(millisecondsTimeout, cancellationToken);
    }

    #region Non Public

    /// <summary>
    /// Потокобезопасно создает следующий уникальный тег.
    /// </summary>
    /// <remarks>От 0 до 65535.</remarks>
    internal string CreateUniqueTag()
    {
        // Создать уникальный tag.
        var intTag = unchecked((ushort)Interlocked.Increment(ref _tagIdSequence));
        return intTag.ToString(CultureInfo.InvariantCulture);
    }

    private static TcpClient ConnectTcp(string host, int port)
    {
        var tcp = new TcpClient();
        try
        {
            tcp.Connect(host, port);
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

    private static Stream AuthenticateSsl(Stream stream, string hostname)
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
            if (sslStream != null)
            {
                await sslStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task LoginPlainAsync(MtOpenConnection socket, string login, string password, CancellationToken cancellationToken)
    {
        var command = new MikroTikCommand("/login")
            .Attribute("name", login)
            .Attribute("password", password);

        _ = await socket.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
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

        var bHash = new byte[16];
        for (var i = 0; i < 16; i++)
        {
            bHash[i] = Convert.ToByte(hash.Substring(i * 2, 2), 16);
        }

        var pass = encoding.GetBytes(password);
        var buf = new byte[pass.Length + bHash.Length + 1];

        Array.Copy(pass, 0, buf, 1, pass.Length);
        Array.Copy(bHash, 0, buf, pass.Length + 1, bHash.Length);

        Span<byte> cHash = stackalloc byte[16];
        HashChallengeResponse(buf, cHash);
        
        var sb = new StringBuilder(34, 34);
        sb.Append("00");
        for (var i = 0; i < 16; i++)
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

    private MtOpenConnection ConnectCore(string host, int port, bool useSsl)
    {
        var tcp = ConnectTcp(host, port);
        var tcpStream = tcp.GetStream();
        try
        {
            var finalStream = useSsl
                ? AuthenticateSsl(tcpStream, host)
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
            if (tcpStream != null)
            {
                await tcpStream.DisposeAsync().ConfigureAwait(false);
            }
            tcp?.Dispose();
        }
    }

    /// <summary>Проверят что существует открытое соединение и возвращает его.</summary>
    /// <exception cref="MikroApiConnectionException"/>
    [MemberNotNull(nameof(_openConnection))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MtOpenConnection GetOpenConnection()
    {
        if (_openConnection != null)
        {
            return _openConnection;
        }

        return ThrowHelper.ThrowNotConnected();
    }

    /// <exception cref="MikroApiConnectionException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckNotAuthorized()
    {
        if (_openConnection == null)
        {
            return;
        }
        ThrowHelper.ThrowAlreadyConnected();
    }

    #region Login

    private Task LoginAsync(MtOpenConnection socket, string login, string password, RouterOsVersion version, CancellationToken cancellationToken)
    {
        if (version == RouterOsVersion.PostVersion6Dot43)
        {
            return LoginPlainAsync(socket, login, password, cancellationToken);
        }
        else
        {
            return LoginAsync(socket, login, password, cancellationToken);
        }
    }

    private void Login(MtOpenConnection socket, string login, string password, RouterOsVersion version, CancellationToken cancellationToken)
    {
        if (version == RouterOsVersion.PostVersion6Dot43)
        {
            LoginPlain(socket, login, password, cancellationToken);
        }
        else
        {
            Login(socket, login, password, cancellationToken);
        }
    }

    /// <summary>
    /// Использует MD5.
    /// </summary>
    private void Login(MtOpenConnection socket, string login, string password, CancellationToken cancellationToken)
    {
        var command = new MikroTikCommand("/login");
        var resp = socket.Execute(command, cancellationToken);

        var hash = resp[0]["ret"];
        var response = EncodePassword(password, hash);

        var secondCommand = new MikroTikCommand("/login")
            .Attribute("name", login)
            .Attribute("response", response);

        socket.Execute(secondCommand, cancellationToken);
    }

    private void LoginPlain(MtOpenConnection socket, string login, string password, CancellationToken cancellationToken)
    {
        CheckNotAuthorized();

        var command = new MikroTikCommand("/login")
            .Attribute("name", login)
            .Attribute("password", password);

        socket.Execute(command, cancellationToken);
    }

    /// <summary>
    /// Использует MD5.
    /// </summary>
    private async Task LoginAsync(MtOpenConnection socket, string login, string password, CancellationToken cancellationToken)
    {
        var command = new MikroTikCommand("/login");
        var resp = await socket.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

        var hash = resp[0]["ret"];
        var response = EncodePassword(password, hash);

        var secondCommand = new MikroTikCommand("/login")
            .Attribute("name", login)
            .Attribute("response", response);

        _ = await socket.ExecuteAsync(secondCommand, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void CheckDisposed()
    {
        if (!_disposed)
        {
            return;
        }

        ThrowHelper.ThrowConnectionDisposed();
    }

    private void CheckNotConnected()
    {
        if (Connected)
        {
            throw new InvalidOperationException("Can't change send timeout after connection");
        }
    }

    #endregion
}
