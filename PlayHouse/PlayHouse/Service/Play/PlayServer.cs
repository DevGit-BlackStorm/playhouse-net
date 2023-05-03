using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Communicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Production;
using PlayHouse.Production.Play;
using CommonLib;

namespace PlayHouse.Service.Play
{
    public class PlayServer : IServer
    {
        private readonly CommonOption _commonOption;
        private readonly PlayOption _playOption;
        private Communicator.Communicator? _communicator;

        public PlayServer(CommonOption commonOption, PlayOption playOption)
        {
            _commonOption = commonOption;
            _playOption = playOption;
        }

        public void Start()
        {
            var communicatorOption = new CommunicatorOption.Builder()
                .SetPort(_commonOption.Port)
                .SetServerSystem(_commonOption.ServerSystem!)
                .SetShowQps(_commonOption.ShowQps)
                .Build();

            var bindEndpoint = communicatorOption.BindEndpoint;
            var serviceId = _commonOption.ServiceId;

            PooledBuffer.Init(_commonOption.MaxBufferPoolSize);

            var communicateServer = new XServerCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(), bindEndpoint));
            var communicateClient = new XClientCommunicator(PlaySocketFactory.CreatePlaySocket(new SocketConfig(), bindEndpoint));

            var requestCache = new RequestCache(_commonOption.RequestTimeoutSec);

            var storageClient = new RedisStorageClient(_commonOption.RedisIp, _commonOption.RedisPort);
            storageClient.Connect();

            var serverInfoCenter = new XServerInfoCenter();

            var xSender = new XSender(serviceId, communicateClient, requestCache);

            var nodeId = storageClient.GetNodeId(bindEndpoint);
            var systemPanelImpl = new XSystemPanel(serverInfoCenter, communicateClient, nodeId);
            ControlContext.BaseSender = xSender;
            ControlContext.SystemPanel = systemPanelImpl;

            var playService = new PlayProcessor(serviceId, bindEndpoint, _playOption, communicateClient, requestCache, serverInfoCenter);

            _communicator = new Communicator.Communicator(
                communicatorOption,
                requestCache,
                serverInfoCenter,
                playService,
                storageClient,
                xSender,
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
