using CommonLib;
using PlayHouse.Communicator;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Production.Session;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Shared;
namespace PlayHouse.Service.Session;

public class SessionServer : IServer
{
    private Communicator.Communicator? _communicator;
    private readonly PlayhouseOption _commonOption;
    private readonly SessionOption _sessionOption;

    public SessionServer(PlayhouseOption commonOption, SessionOption sessionOption)
    {
        _commonOption = commonOption;
        _sessionOption = sessionOption;
    }

    public void Start()
    {
        var communicatorOption = new CommunicatorOption.Builder()
                  .SetIp(_commonOption.Ip)
                  .SetPort(_commonOption.Port)
                  .SetServiceProvider(_commonOption.ServiceProvider)
                  .SetShowQps(_commonOption.ShowQps)
                  .SetNodeId(_commonOption.NodeId)
                  .SetPacketProducer(_commonOption.PacketProducer)
                  .SetAddressServerEndpoints(_commonOption.AddressServerEndpoints)
                  .SetAddressServerServiceId(_commonOption.AddressServerServiceId)
                  .SetPacketProducer((msgId,paload,msgSeq)=>new EmptyPacket())
                  .Build();

        PooledBuffer.Init(_commonOption.MaxBufferPoolSize);

        var bindEndpoint = communicatorOption.BindEndpoint;
        ushort serviceId = _commonOption.ServiceId;

        var communicateClient = new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(),bindEndpoint));

        var requestCache = new RequestCache(_commonOption.RequestTimeoutSec);

        var serverInfoCenter = new XServerInfoCenter();

        var sessionService = new SessionService(
            serviceId,
            _sessionOption,
            serverInfoCenter,
            communicateClient,
            requestCache,
            _commonOption.ShowQps
        );

        _communicator = new Communicator.Communicator(
            communicatorOption,
            requestCache,
            serverInfoCenter,
            sessionService,
            communicateClient
        );
        _communicator.Start();
    }

    public async Task StopAsync()
    {
        await _communicator!.StopAsync();
    }

    public void AwaitTermination()
    {
        _communicator!.AwaitTermination();
    }
}
