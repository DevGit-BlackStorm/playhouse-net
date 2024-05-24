using PlayHouse.Communicator;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Session;

internal class TargetServiceCache(IServerInfoCenter serverInfoCenter)
{
    private readonly Dictionary<ushort, ServiceType> _targetedService = new();

    public ServiceType FindTypeBy(ushort serviceId)
    {
        if (!_targetedService.TryGetValue(serviceId, out var type))
        {
            type = serverInfoCenter.FindServerType(serviceId);
            _targetedService[serviceId] = type;
        }

        return type;
    }
}