using PlayHouse.Communicator;
using PlayHouse.Communicator.PlaySocket;

namespace PlayHouse.Service.Session
{
    class SessionServer : IServer
    {
        private Communicator.Communicator? _communicator;

        private readonly CommonOption _commonOption;
        private readonly SessionOption _sessionOption;

        public SessionServer(CommonOption commonOption, SessionOption sessionOption)
        {
            _commonOption = commonOption;
            _sessionOption = sessionOption;
        }

        public void Start()
        {
            var communicatorOption = new CommunicatorOption.Builder()
                .SetPort(_commonOption.Port)
                .SetServerSystem(_commonOption.ServerSystem!)
                .SetShowQps(_commonOption.ShowQps)
                .Build();

            var bindEndpoint = communicatorOption.BindEndpoint;
            short serviceId = _commonOption.ServiceId;

            var communicateServer = new XServerCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(),bindEndpoint));
            var communicateClient = new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(),bindEndpoint));

            var requestCache = new RequestCache(_commonOption.RequestTimeoutSec);

            var storageClient = new RedisStorageClient(_commonOption.RedisIp, _commonOption.RedisPort);
            storageClient.Connect();

            var serverInfoCenter = new XServerInfoCenter();

            var baseSenderImpl = new BaseSender(serviceId, communicateClient, requestCache);
            var systemPanelImpl = new BaseSystemPanel(serverInfoCenter, communicateClient);

            ControlContext.BaseSender = baseSenderImpl;
            ControlContext.SystemPanel = systemPanelImpl;

            var sessionService = new SessionService(
                serviceId,
                _sessionOption,
                serverInfoCenter,
                communicateClient,
                requestCache,
                _sessionOption.SessionPort,
                _commonOption.ShowQps
            );

            _communicator = new Communicator.Communicator(
                communicatorOption,
                requestCache,
                serverInfoCenter,
                sessionService,
                storageClient,
                baseSenderImpl,
                systemPanelImpl,
                communicateServer,
                communicateClient
            );
            _communicator.Start();
        }

        public void Stop()
        {
            _communicator!.Stop();
        }

        public void AwaitTermination()
        {
            _communicator!.AwaitTermination();
        }
    }


}
