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

        var communicatorOption = new CommunicatorOption.Builder()
            .SetIp(commonOption.Ip)
            .SetPort(commonOption.Port)
            .SetServiceProvider(commonOption.ServiceProvider)
            .SetShowQps(commonOption.ShowQps)
            .SetNodeId(commonOption.NodeId)
            .SetPacketProducer(commonOption.PacketProducer)
            .Build();

        var bindEndpoint = communicatorOption.BindEndpoint;
        var serviceId = commonOption.ServiceId;

        PooledBuffer.Init(commonOption.MaxBufferPoolSize);

        var communicateClient =
            new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(), bindEndpoint));

        var requestCache = new RequestCache(commonOption.RequestTimeoutSec);
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