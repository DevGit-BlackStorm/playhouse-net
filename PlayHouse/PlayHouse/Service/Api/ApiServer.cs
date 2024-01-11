﻿using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Communicator;
using PlayHouse.Production.Api;
using CommonLib;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Api;
public class ApiServer : IServer
{
    private readonly CommonOption _commonOption;
    private readonly ApiOption _apiOption;
    private Communicator.Communicator? _communicator;

    public ApiServer(
        CommonOption commonOption, 
        ApiOption apiOption )
    {
        _commonOption = commonOption;
        _apiOption = apiOption;
    }

    public void Start()
    {

        var communicatorOption = new CommunicatorOption.Builder()
                .SetPort(_commonOption.Port)
                .SetServiceProvider(_commonOption.ServiceProvider)
                .SetShowQps(_commonOption.ShowQps)
                .SetNodeId(_commonOption.NodeId)
                .SetPacketProducer(_commonOption.PacketProducer)
                .SetAddressServerEndpoints(_commonOption.AddressServerEndpoints)
                .SetAddressServerServiceId(_commonOption.AddressServerServiceId)
                .Build();

        var bindEndpoint = communicatorOption.BindEndpoint;
        var serviceId = _commonOption.ServiceId;

        
        PooledBuffer.Init(_commonOption.MaxBufferPoolSize);

        var requestCache = new RequestCache(_commonOption.RequestTimeoutSec);
        var serverInfoCenter = new XServerInfoCenter();
        
        var communicateClient = new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(), bindEndpoint));

        var service = new ApiService(serviceId, _apiOption,  requestCache, communicateClient,  communicatorOption.ServiceProvider);

        _communicator = new Communicator.Communicator(
            communicatorOption,
            requestCache,
            serverInfoCenter,
            service,
            communicateClient
        );

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
