using CommonLib;
using PlayHouse.Communicator;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Production.Api;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Api;

public class ApiServer : IServer
{
    private readonly Communicator.Communicator? _communicator;

    public ApiServer(
        PlayhouseOption commonOption,
        ApiOption apiOption)
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

        var requestCache = new RequestCache(commonOption.RequestTimeoutSec);
        var serverInfoCenter = new XServerInfoCenter(commonOption.DebugMode);

        var communicateClient =
            new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(), bindEndpoint));

        var service = new ApiService(serviceId, apiOption, requestCache, communicateClient,
            communicatorOption.ServiceProvider);

        _communicator = new Communicator.Communicator(
            communicatorOption,
            requestCache,
            serverInfoCenter,
            service,
            communicateClient
        );
    }

    public void Start()
    {
        _communicator!.Start();
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