using PlayHouse.Communicator;

namespace PlayHouse.Service
{
    public class BaseSystemPanel : ISystemPanel
    {
        private readonly IServerInfoCenter serverInfoCenter;
        private readonly IClientCommunicator clientCommunicator;

        public Communicator.Communicator? Communicator { get; set; }

        public BaseSystemPanel(IServerInfoCenter serverInfoCenter, IClientCommunicator clientCommunicator)
        {
            this.serverInfoCenter = serverInfoCenter;
            this.clientCommunicator = clientCommunicator;
        }

        public IServerInfo RandomServerInfo(short serviceId)
        {
            return serverInfoCenter.FindRoundRobinServer(serviceId);
        }

        public IServerInfo ServerInfo(string endpoint)
        {
            return serverInfoCenter.FindServer(endpoint);
        }

        public IList<IServerInfo> ServerList()
        {
            return serverInfoCenter.GetServerList().Cast<IServerInfo>().ToList();
        }

        public void Pause()
        {
            Communicator!.Pause();
        }

        public void Resume()
        {
            Communicator!.Resume();
        }

        public void Shutdown()
        {
            Communicator!.Stop();
        }

        public ServerState ServerState()
        {
            return Communicator!.GetServerState();
        }
    }
}
