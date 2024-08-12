using Microsoft.Extensions.DependencyInjection;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

public class CommunicatorOption
{
    private CommunicatorOption(
        string bindEndpoint,
        IServiceProvider serviceProvider,
        bool showQps,
        int nodeId,
        Func<string, IPayload, ushort, IPacket> packetProducer
    )
    {
        BindEndpoint = bindEndpoint;
        ShowQps = showQps;
        NodeId = nodeId;
        ServiceProvider = serviceProvider;
        PacketProducer = packetProducer;
    }

    public string BindEndpoint { get; }
    public bool ShowQps { get; }
    public int NodeId { get; }
    public IServiceProvider ServiceProvider { get; }
    public Func<string, IPayload, ushort, IPacket>? PacketProducer { get; }


    public class Builder
    {
        private string _Ip = string.Empty;
        private int _nodeId;
        private Func<string, IPayload, ushort, IPacket>? _packetProducer;
        private int _port;
        private IServiceProvider? _serviceProvider;
        private bool _showQps;
        private ISystemController? _system;

        public Builder SetIp(string ip)
        {
            _Ip = ip;
            return this;
        }

        public Builder SetPort(int port)
        {
            _port = port;
            return this;
        }


        public Builder SetShowQps(bool showQps)
        {
            _showQps = showQps;
            return this;
        }

        public Builder SetServiceProvider(IServiceProvider? serviceProvider)
        {
            _serviceProvider = serviceProvider;
            return this;
        }

        public CommunicatorOption Build()
        {
            var localIp = IpFinder.FindLocalIp();
            if (_Ip != string.Empty)
            {
                localIp = _Ip;
            }


            var bindEndpoint = $"tcp://{localIp}:{_port}";

            if (_serviceProvider == null)
            {
                throw new Exception("serviceProvider is not registered");
            }

            if (_packetProducer == null)
            {
                throw new Exception("packetProducer is not registered");
            }

            return new CommunicatorOption(
                bindEndpoint,
                _serviceProvider!,
                _showQps,
                _nodeId,
                _packetProducer
            );
        }

        public Builder SetNodeId(int nodeId)
        {
            if (nodeId >= 0 && nodeId < 4096)
            {
                _nodeId = nodeId;
            }
            else
            {
                throw new Exception("invalid nodeId , ");
            }

            return this;
        }

        public Builder SetPacketProducer(Func<string, IPayload, ushort, IPacket>? producer)
        {
            _packetProducer = producer;
            return this;
        }

        //internal Builder SetAddressServerServiceId(ushort updateServerServiceId)
        //{
        //    if (updateServerServiceId <= 0)
        //    {
        //        throw new Exception("invalid updateServerServiceId");
        //    }

        //    _addressServerId = updateServerServiceId;
        //    return this;
        //}

        //internal Builder SetAddressServerEndpoints(List<string> addressServerEndpoints)
        //{
        //    if (addressServerEndpoints == null || addressServerEndpoints.Count == 0)
        //    {
        //        throw new Exception("no registered addressServerEndpoint");
        //    }

        //    _addressServerEndpoints = addressServerEndpoints.Select(e => $"tcp://{e}").ToList();
        //    return this;
        //}
    }
}

internal class Communicator : ICommunicateListener
{
    private readonly ServerAddressResolver _addressResolver;
    private readonly XClientCommunicator _clientCommunicator;
    private readonly LOG<Communicator> _log = new();
    private readonly MessageLoop _messageLoop;
    private readonly CommunicatorOption _option;
    private readonly PerformanceTester _performanceTester;
    private readonly RequestCache _requestCache;

    private readonly XSender _sender;
    private readonly XServerCommunicator _serverCommunicator;
    private readonly XServerInfoCenter _serverInfoCenter;
    private readonly IService _service;
    private readonly ushort _serviceId;
    private readonly SystemDispatcher _systemDispatcher;
    private readonly XSystemPanel _systemPanel;

    public Communicator(
        CommunicatorOption option,
        RequestCache requestCache,
        XServerInfoCenter serverInfoCenter,
        IService service,
        XClientCommunicator clientCommunicator
    )
    {
        _option = option;
        _requestCache = requestCache;
        _serverInfoCenter = serverInfoCenter;
        _service = service;
        _clientCommunicator = clientCommunicator;
        _serviceId = _service.ServiceId;

        _serverCommunicator =
            new XServerCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(), _option.BindEndpoint));
        _performanceTester = new PerformanceTester(_option.ShowQps);
        _messageLoop = new MessageLoop(_serverCommunicator, _clientCommunicator);
        _sender = new XSender(_serviceId, _clientCommunicator, _requestCache);
        _systemPanel = new XSystemPanel(_serverInfoCenter, _clientCommunicator, _option.NodeId, _option.BindEndpoint);

        var systemController = _option.ServiceProvider.GetRequiredService<ISystemController>();

        _addressResolver = new ServerAddressResolver(
            _option.BindEndpoint,
            _serverInfoCenter,
            _clientCommunicator,
            _service,
            systemController
            );
        _systemDispatcher = new SystemDispatcher(_serviceId, _requestCache, _clientCommunicator, _systemPanel,
            option.ServiceProvider);

        ControlContext.Init(_sender, _systemPanel);
        PacketProducer.Init(_option.PacketProducer!);
    }

    public void OnReceive(RoutePacket routePacket)
    {
        _performanceTester.IncCounter();

        Dispatch(routePacket);
        //Task.Run(async () =>  { await Dispatch(routePacket); });
    }

    public void Start()
    {
        var bindEndpoint = _option.BindEndpoint;
        _systemPanel.Communicator = this;

        _serverCommunicator.Bind(this);

        _messageLoop.Start();

        _clientCommunicator.Connect(bindEndpoint);


        _addressResolver.Start();

        _service.OnStart();
        _performanceTester.Start();
        _systemDispatcher.Start();

        _log.Info(() => $"============== server start ==============");
        _log.Info(() => $"Ready for bind: {bindEndpoint}");
    }

    public async Task StopAsync()
    {
        _service.OnStop();

        await Task.Delay(ConstOption.StopDelayMs);

        _performanceTester.Stop();
        _addressResolver.Stop();
        _messageLoop.Stop();
        _systemDispatcher.Stop();

        _log.Info(() => $"============== server stop ==============");
    }

    public void AwaitTermination()
    {
        _messageLoop.AwaitTermination();
    }

    private void Dispatch(RoutePacket routePacket)
    {
        try
        {
            //PacketContext.AsyncCore.Init();
            //ServiceAsyncContext.Init();

            if (routePacket.IsBackend() && routePacket.IsReply())
            {
                _requestCache.OnReply(routePacket);
                return;
            }

            if (routePacket.IsSystem)
            {
                _systemDispatcher.OnPost(routePacket);
            }
            else
            {
                _service.OnPost(routePacket);
            }
        }
        catch (ServiceException.NotRegisterMethod e)
        {
            var sender = new XSender(_serviceId, _clientCommunicator, _requestCache);
            sender.SetCurrentPacketHeader(routePacket.RouteHeader);

            if (routePacket.Header.MsgSeq > 0)
            {
                sender.Reply((ushort)BaseErrorCode.NotRegisteredMessage);
            }

            _log.Error(() => $"{e.Message}");
        }
        catch (ServiceException.NotRegisterInstance e)
        {
            var sender = new XSender(_serviceId, _clientCommunicator, _requestCache);
            sender.SetCurrentPacketHeader(routePacket.RouteHeader);

            if (routePacket.Header.MsgSeq > 0)
            {
                sender.Reply((ushort)BaseErrorCode.SystemError);
            }

            _log.Error(() => $"{e.Message}");
        }
        catch (Exception e)
        {
            var sender = new XSender(_serviceId, _clientCommunicator, _requestCache);
            sender.SetCurrentPacketHeader(routePacket.RouteHeader);
            // Use this error code when it's set in the content.
            // Use the default content error code if it's not set in the content.
            if (routePacket.Header.MsgSeq > 0)
            {
                sender.Reply((ushort)BaseErrorCode.UncheckedContentsError);
            }

            _log.Error(() => $"Packet processing failed due to an unexpected error. - [msgId:{routePacket.MsgId}]");
            _log.Error(() =>$"[exception message:{e.Message}]");
            _log.Error(() => $"[exception message:{e.StackTrace}]");

            if (e.InnerException != null)
            {
                _log.Error(() => $"[internal exception message:{e.InnerException.Message}");
                _log.Error(() => $"[internal exception trace:{e.InnerException.StackTrace}");
            }
        }
    }

    public void Pause()
    {
        _service.OnPause();
    }

    public void Resume()
    {
        _service.OnResume();
    }

    public ServerState GetServerState()
    {
        return _service.GetServerState();
    }
}