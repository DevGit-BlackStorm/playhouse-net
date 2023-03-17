using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    class ServerAddressResolver
    {
        private readonly string bindEndpoint;
        private readonly XServerInfoCenter serverInfoCenter;
        private readonly XClientCommunicator communicateClient;
        private readonly IService service;
        private readonly IStorageClient storageClient;

        private Timer? _timer;

        public ServerAddressResolver(string bindEndpoint, XServerInfoCenter serverInfoCenter,
            XClientCommunicator communicateClient, IService service, IStorageClient storageClient)
        {
            this.bindEndpoint = bindEndpoint;
            this.serverInfoCenter = serverInfoCenter;
            this.communicateClient = communicateClient;
            this.service = service;
            this.storageClient = storageClient;
        }

        public void Start()
        {
            LOG.Info("Server address resolver start", this.GetType());

            _timer = new Timer(_ =>
            {
                try
                {
                    storageClient.UpdateServerInfo(new XServerInfo(
                        bindEndpoint,
                        service.ServiceType(),
                        service.ServiceId(),
                        service.ServerState(),
                        service.WeightPoint(),
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    ));

                    IList<XServerInfo> serverInfoList = storageClient.GetServerList(bindEndpoint);
                    IList<XServerInfo> updateList = serverInfoCenter.Update(serverInfoList);

                    foreach (XServerInfo serverInfo in updateList)
                    {
                        switch (serverInfo.State)
                        {
                            case ServerState.RUNNING:
                                communicateClient.Connect(serverInfo.BindEndpoint);
                                break;
                            case ServerState.DISABLE:
                                communicateClient.Disconnect(serverInfo.BindEndpoint);
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    LOG.Error(e.ToString(), this.GetType(), e);
                }
            }, null, ConstOption.ADDRESS_RESOLVER_INITIAL_DELAY, ConstOption.ADDRESS_RESOLVER_PERIOD);
            
            
        }

        public void Stop()
        {
            _timer?.Dispose();
        }
    }

}
