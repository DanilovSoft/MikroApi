using System;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    public interface IMikroTikConnection
    {
        bool Connected { get; }
        
        int ReceiveTimeout { get; set; }
        
        int SendTimeout { get; set; }

        void CancelListeners();
        
        void CancelListeners(bool wait);
        
        Task CancelListenersAsync();
        
        Task CancelListenersAsync(bool wait);
        
        MikroTikFlowCommand Command(string command);

        /// <exception cref="MikroApiConnectionException"/>
        /// <exception cref="ObjectDisposedException"/>
        void Connect(string login, string password, string hostname, int port = 8728);

        /// <exception cref="MikroApiConnectionException"/>
        /// <exception cref="ObjectDisposedException"/>
        void Connect(string login, string password, string hostname, int port = 8728, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43);

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="MikroApiConnectionException"/>
        /// <exception cref="ObjectDisposedException"/>
        void ConnectSsl(string login, string password, string hostname, int port = 8729);

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="MikroApiConnectionException"/>
        /// <exception cref="ObjectDisposedException"/>
        void ConnectSsl(string login, string password, string hostname, int port = 8729, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43);

        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        Task ConnectAsync(string login, string password, string hostname, int port = 8728, CancellationToken cancellationToken = default);

        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        Task ConnectAsync(string login, string password, string hostname, int port = 8728,
                          RouterOsVersion version = RouterOsVersion.PostVersion6Dot43,
                          CancellationToken cancellationToken = default);

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        Task ConnectSslAsync(string login, string password, string hostname, int port = 8729, CancellationToken cancellationToken = default);

        /// <summary>
        /// Для api-ssl.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        Task ConnectSslAsync(string login, string password, string hostname, int port = 8729,
                             RouterOsVersion version = RouterOsVersion.PostVersion6Dot43,
                             CancellationToken cancellationToken = default);
        void Dispose();

        MikroTikResponseListener Listen(MikroTikCommand command);
        
        Task<MikroTikResponseListener> ListenAsync(MikroTikCommand command);
        
        bool Quit(int millisecondsTimeout);
        
        Task<bool> QuitAsync(int millisecondsTimeout);
        
        MikroTikResponse Send(MikroTikCommand command);
        
        Task<MikroTikResponse> SendAsync(MikroTikCommand command);
    }
}