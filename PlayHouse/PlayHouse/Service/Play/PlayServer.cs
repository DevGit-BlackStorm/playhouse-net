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
            .SetNid(commonOption.Nid)
            .SetPacketProducer(commonOption.PacketProducer)
            .Build();

        var nid = communicatorOption.Nid;
        var bindEndpoint = communicatorOption.BindEndpoint;
        var serviceId = commonOption.ServiceId;

        PooledBuffer.Init(commonOption.MaxBufferPoolSize);
        var playSocketConfig = commonOption.PlaySocketConfig;
        var communicateClient =
            new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(nid, bindEndpoint, playSocketConfig)));

        var requestCache = new RequestCache(commonOption.RequestTimeoutSec);
        var serverInfoCenter = new XServerInfoCenter(commonOption.DebugMode);
        var playService = new PlayService(serviceId,nid, playOption, communicateClient, requestCache,
            serverInfoCenter);

        _communicator = new Communicator.Communicator(
            communicatorOption,
            playSocketConfig,
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