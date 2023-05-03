using PlayHouse.Communicator;
using PlayHouse.Production;

namespace PlayHouse.Service.Session
{
    class TargetServiceCache
    {
        private readonly IServerInfoCenter _serverInfoCenter;
        private readonly Dictionary<short, ServiceType> _targetedService = new Dictionary<short, ServiceType>();

        public TargetServiceCache(IServerInfoCenter serverInfoCenter)
        {
            _serverInfoCenter = serverInfoCenter;
        }

        public ServiceType FindTypeBy(short serviceId)
        {
            if (!_targetedService.TryGetValue(serviceId, out ServiceType type))
            {
                type = _serverInfoCenter.FindServerType(serviceId);
                _targetedService[serviceId] = type;
            }
            return type;
        }
    }

}