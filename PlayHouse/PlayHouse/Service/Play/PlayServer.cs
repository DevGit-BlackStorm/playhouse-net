using CommonLib;
using PlayHouse.Communicator;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Production.Play;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play;

public class PlayServer : IServer
{
    private readonly Communicator.Communicator _communicator;

    public PlayServer(PlayhouseOption commonOption, PlayOption playOption)
    {
        var commonOption1 = commonOption;

        var communicatorOption = new CommunicatorOption.Builder()
            .SetIp(commonOption1.Ip)
            .SetPort(commonOption1.Port)
            .SetServiceProvider(commonOption1.ServiceProvider)
            .SetShowQps(commonOption1.ShowQps)
            .SetNodeId(commonOption1.NodeId)
            .SetPacketProducer(commonOption1.PacketProducer)
            .SetAddressServerEndpoints(commonOption1.AddressServerEndpoints)
            .SetAddressServerServiceId(commonOption1.AddressServerServiceId)
            .Build();

        var bindEndpoint = communicatorOption.BindEndpoint;
        var serviceId = commonOption1.ServiceId;

        PooledBuffer.Init(commonOption1.MaxBufferPoolSize);

        var communicateClient =
            new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(), bindEndpoint));

        var requestCache = new RequestCache(commonOption1.RequestTimeoutSec);
        var serverInfoCenter = new XServerInfoCenter();
        var playService = new PlayService(serviceId, bindEndpoint, playOption, communicateClient, requestCache,
            serverInfoCenter);

        _communicator = new Communicator.Communicator(
            communicatorOption,
            requestCache,
            serverInfoCenter,
            playService,
            communicateClient
        );
    }

    public void Start()
    {
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