using PlayHouse.Production.Shared;
using System.Collections.Immutable;
using System.Diagnostics;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

using System.Collections.Immutable;
using System.Threading;

internal class XServerInfoCenter(bool debugMode) : IServerInfoCenter
{
    private int _offset;
    private ImmutableList<XServerInfo> _serverInfoList = ImmutableList<XServerInfo>.Empty;
    private LOG<XServerCommunicator> _log = new();

    public IReadOnlyList<XServerInfo> Update(IReadOnlyList<XServerInfo> serverList)
    {
        if (serverList.Count == 0)
        {
            return Volatile.Read(ref _serverInfoList);
        }

        foreach (var xServerInfo in serverList)
        {
            if (!debugMode)
            {
                xServerInfo.CheckTimeout();
            }
        }

        var newList = serverList.OrderBy(x => x.GetBindEndpoint()).ToImmutableList();

        // Volatile.Write를 사용하여 원자적으로 리스트 교체
        Volatile.Write(ref _serverInfoList, newList);

        return Volatile.Read(ref _serverInfoList);
    }

    public XServerInfo FindServer(string endpoint)
    {
        var serverInfo = Volatile.Read(ref _serverInfoList)
            .FirstOrDefault(e => e.IsValid() && e.GetBindEndpoint() == endpoint);

        if (serverInfo == null)
        {
            throw new CommunicatorException.NotExistServerInfo($"target endpoint:{endpoint} , ServerInfo is not exist");
        }
        
        return serverInfo;
    }

    public XServerInfo FindRoundRobinServer(ushort serviceId)
    {
        var list = Volatile.Read(ref _serverInfoList)
            .Where(x => x.IsValid() && x.GetServiceId() == serviceId)
            .ToList();

        if (!list.Any())
        {
            throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
        }

        var next = Interlocked.Increment(ref _offset);
        if (next < 0)
        {
            next *= -1;
        }

        var index = next % list.Count;
        return list[index];
    }

    public IReadOnlyList<XServerInfo> GetServerList()
    {
        return Volatile.Read(ref _serverInfoList);
    }

    public XServerInfo FindServerByAccountId(ushort serviceId, long accountId)
    {
        var list = Volatile.Read(ref _serverInfoList)
            .Where(e => e.IsValid() && e.GetServiceId() == serviceId)
            .ToList();

        if (list.Count == 0)
        {
            throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
        }

        var index = (int)(accountId % list.Count);
        return list[index];
    }

    public ServiceType FindServerType(ushort serviceId)
    {
        var list = Volatile.Read(ref _serverInfoList)
            .Where(info => info.GetServiceId() == serviceId)
            .ToList();

        if (list.Count == 0)
        {
            throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
        }

        return list.First().GetServiceType();
    }
}
