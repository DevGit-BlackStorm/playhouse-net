using PlayHouse.Communicator;

namespace PlayHouse.Service.Session
{
    internal class TargetServiceCache
    {
        private readonly IServerInfoCenter serverInfoCenter;
        private readonly Dictionary<short, XServerInfo> targetedService = new Dictionary<short, XServerInfo>();

        public TargetServiceCache(IServerInfoCenter serverInfoCenter)
        {
            this.serverInfoCenter = serverInfoCenter;
        }

        public XServerInfo FindServer(short serviceId)
        {
            if (targetedService.TryGetValue(serviceId, out var findServer) && findServer.IsValid())
            {
                return findServer;
            }
            else
            {
                findServer = serverInfoCenter.FindRoundRobinServer(serviceId);
                targetedService[serviceId] = findServer;
                return findServer;
            }
        }

        public List<XServerInfo> GetTargetedServers()
        {
            return targetedService.Values.ToList();
        }
    }

}