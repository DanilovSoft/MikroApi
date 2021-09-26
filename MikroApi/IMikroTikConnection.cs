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

        /// <param name="useSsl">Для <c>api-ssl</c>.</param>
        /// <exception cref="MikroApiConnectionException"/>
        /// <exception cref="ObjectDisposedException"/>
        void Connect(string login, string password, string hostname, bool useSsl, int port = 8728);

        /// <param name="useSsl">Для <c>api-ssl</c>.</param>
        /// <exception cref="MikroApiConnectionException"/>
        /// <exception cref="ObjectDisposedException"/>
        void Connect(string login, string password, string hostname, bool useSsl, int port = 8728, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43);

        /// <param name="useSsl">Для <c>api-ssl</c>.</param>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        Task ConnectAsync(string login, string password, string hostname, bool useSsl, int port = 8728, CancellationToken cancellationToken = default);

        /// <param name="useSsl">Для <c>api-ssl</c>.</param>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="OperationCanceledException"/>
        Task ConnectAsync(string login, string password, string hostname, bool useSsl, int port = 8728, RouterOsVersion version = RouterOsVersion.PostVersion6Dot43,
                          CancellationToken cancellationToken = default);

        void CancelListeners();

        void CancelListeners(bool wait);

        Task CancelListenersAsync();

        Task CancelListenersAsync(bool wait);

        MikroTikFlowCommand Command(string command);

        void Dispose();

        MikroTikResponseListener Listen(MikroTikCommand command);
        
        Task<MikroTikResponseListener> ListenAsync(MikroTikCommand command);
        
        bool Quit(int millisecondsTimeout);
        
        Task<bool> QuitAsync(int millisecondsTimeout);
        
        MikroTikResponse Send(MikroTikCommand command);
        
        Task<MikroTikResponse> SendAsync(MikroTikCommand command);
    }
}