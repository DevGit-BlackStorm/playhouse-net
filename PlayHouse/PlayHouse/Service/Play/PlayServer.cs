using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Communicator;
using PlayHouse.Production.Play;
using CommonLib;
using PlayHouse.Service.Shared;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play
{
    public class PlayServer : IServer
    {
        private readonly PlayhouseOption _commonOption;
        private readonly PlayOption _playOption;
        private Communicator.Communicator? _communicator;

        public PlayServer(PlayhouseOption commonOption, PlayOption playOption)
        {
            _commonOption = commonOption;
            _playOption = playOption;
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
                .Build();

            var bindEndpoint = communicatorOption.BindEndpoint;
            var serviceId = _commonOption.ServiceId;

            PooledBuffer.Init(_commonOption.MaxBufferPoolSize);

            var communicateClient = new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(), bindEndpoint));

            var requestCache = new RequestCache(_commonOption.RequestTimeoutSec);
            var serverInfoCenter = new XServerInfoCenter();
            var playService = new PlayService(serviceId, bindEndpoint, _playOption, communicateClient, requestCache, serverInfoCenter);

            _communicator = new Communicator.Communicator(
                communicatorOption,
                requestCache,
                serverInfoCenter,
                playService,
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

}
