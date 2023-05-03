using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Service;
using System.Net;

namespace PlayHouse.Communicator
{
    public delegate IServerSystem ServerSystemFactory(ISystemPanel systemPanel, ISender baseSender);
    public class CommunicatorOption
    {
        public string BindEndpoint { get; }
        public ServerSystemFactory ServerSystem { get; }
        public bool ShowQps { get; }

        private CommunicatorOption(string bindEndpoint, ServerSystemFactory serverSystem, bool showQps)
        {
            BindEndpoint = bindEndpoint;
            ServerSystem = serverSystem;
            ShowQps = showQps;
        }

        public class Builder
        {
            private int _port;
            private ServerSystemFactory? _serverSystem;
            private bool _showQps;

            public Builder SetPort(int port)
            {
                _port = port;
                return this;
            }

            public Builder SetServerSystem(ServerSystemFactory serverSystem)
            {
                _serverSystem = serverSystem;
                return this;
            }

            public Builder SetShowQps(bool showQps)
            {
                _showQps = showQps;
                return this;
            }

            public CommunicatorOption Build()
            {
                var localIp = IpFinder.FindLocalIp();
                var bindEndpoint = $"tcp://{localIp}:{_port}";
                return new CommunicatorOption(bindEndpoint, _serverSystem!, _showQps);
            }
        }
    }

    public class Communicator : ICommunicateListener
    {
        private readonly CommunicatorOption _option;
        private readonly RequestCache _requestCache;
        private readonly XServerInfoCenter _serverInfoCenter;
        private readonly IProcessor _service;
        private IStorageClient _storageClient;
        private readonly XSender _baseSender;
        private readonly XSystemPanel _systemPanel;
        private readonly XServerCommunicator _communicateServer;
        private readonly XClientCommunicator _communicateClient;

        private MessageLoop _messageLoop;
        private ServerAddressResolver _addressResolver;
        private BaseSystem _baseSystem;
        private readonly PerformanceTester _performanceTester;

        public Communicator(
            CommunicatorOption option,
            RequestCache requestCache,
            XServerInfoCenter serverInfoCenter,
            IProcessor service,
            IStorageClient storageClient,
            XSender baseSender,
            XSystemPanel systemPanel,
            XServerCommunicator communicateServer,
            XClientCommunicator communicateClient)
        {
            _option = option;
            _requestCache = requestCache;
            _serverInfoCenter = serverInfoCenter;
            _service = service;
            _storageClient = storageClient;
            _baseSender = baseSender;
            _systemPanel = systemPanel;
            _communicateServer = communicateServer;
            _communicateClient = communicateClient;
            _performanceTester = new PerformanceTester(_option.ShowQps);

            _messageLoop = new MessageLoop(_communicateServer, _communicateClient);

            var bindEndpoint = _option.BindEndpoint;
            var system = _option.ServerSystem;

            _addressResolver = new ServerAddressResolver(
                                    bindEndpoint,
                                    _serverInfoCenter,
                                    _communicateClient,
                                    _service,
                                    _storageClient);
                        

            _baseSystem = new BaseSystem(system.Invoke(_systemPanel, _baseSender), _baseSender);
        }

        public void Start()
        {
            var bindEndpoint = _option.BindEndpoint;
            var system = _option.ServerSystem;
            _systemPanel.Communicator = this;

            _communicateServer.Bind(this);

            _messageLoop.Start();
            _addressResolver.Start();
            _baseSystem.Start();

            _service.OnStart();
            _performanceTester.Start();

            LOG.Info("============== server start ==============", this.GetType());
            LOG.Info($"Ready for bind: {bindEndpoint}", this.GetType());
        }

        private void UpdateDisable()
        {

            XServerInfo serverInfo = XServerInfo.Of(_option.BindEndpoint, _service);
            serverInfo.State = ServerState.DISABLE;
            
            _storageClient.UpdateServerInfo(serverInfo);
        }

        public void Stop()
        {
            _performanceTester.Stop();
            UpdateDisable();
            _baseSystem.Stop();
            _addressResolver.Stop();
            _messageLoop.Stop();

            LOG.Info("============== server stop ==============", this.GetType());
        }

        public void AwaitTermination()
        {
            _messageLoop.AwaitTermination();
        }


        public void OnReceive(RoutePacket routePacket)
        {
            LOG.Trace($"onReceive: from:{routePacket.RouteHeader.From}, packetInfo:${routePacket.RouteHeader}", this.GetType());

            _performanceTester.IncCounter();

            if (!routePacket.ForClient() && routePacket.IsReply())
            {
                _requestCache.OnReply(routePacket);
                return;
            }

            if (routePacket.IsSystem())
            {
                _baseSystem.OnReceive(routePacket);
            }
            else
            {
                _service.OnReceive(routePacket);
            }
        }

        public void Pause()
        {
            _service.Pause();
            _baseSystem.Pause();
        }

        public void Resume()
        {
            _service.Resume();
            _baseSystem.Resume();
        }

        public ServerState GetServerState()
        {
            return _service.GetServerState();
        }
    }

}
