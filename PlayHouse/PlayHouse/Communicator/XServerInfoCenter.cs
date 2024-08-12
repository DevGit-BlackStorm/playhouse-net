using System.Collections.Concurrent;
using PlayHouse.Production.Shared;

namespace PlayHouse.Communicator;

internal class XServerInfoCenter : IServerInfoCenter
{
    //private readonly IDictionary<string, XServerInfo> _serverInfoMap = new ConcurrentDictionary<string, XServerInfo>();
    private int _offset;
    private List<XServerInfo> _serverInfoList = new();

    public IReadOnlyList<XServerInfo> Update(IReadOnlyList<XServerInfo> serverList)
    {
        //var updatedMap = new Dictionary<string, XServerInfo>();
        //foreach (var newInfo in serverList)
        //{
        //    newInfo.CheckTimeout();

        //    if (_serverInfoMap.TryGetValue(newInfo.GetBindEndpoint(), out var oldInfo))
        //    {
        //        if (oldInfo.Update(newInfo))
        //        {
        //            updatedMap[newInfo.GetBindEndpoint()] = newInfo;
        //        }
        //    }
        //    else
        //    {
        //        _serverInfoMap[newInfo.GetBindEndpoint()] = newInfo;
        //        updatedMap[newInfo.GetBindEndpoint()] = newInfo;
        //    }
        //}

        //// Remove server info if it's not in the list
        //foreach (var oldInfo in _serverInfoMap.Values.ToList())
        //{
        //    if (oldInfo.CheckTimeout())
        //    {
        //        updatedMap[oldInfo.GetBindEndpoint()] = oldInfo;
        //    }
        //}

        //_serverInfoList = _serverInfoMap.Values.ToList().OrderBy(x => x.GetBindEndpoint()).ToList();

        //return updatedMap.Values.ToList();

        foreach (var xServerInfo in serverList)
        {
            xServerInfo.CheckTimeout();
        }

        _serverInfoList = serverList.OrderBy(x => x.GetBindEndpoint()).ToList();

        return _serverInfoList;
    }

    public XServerInfo FindServer(string endpoint)
    {
        //if (!_serverInfoMap.TryGetValue(endpoint, out var serverInfo) || !serverInfo.IsValid())
        //{
        //    throw new CommunicatorException.NotExistServerInfo($"target endpoint:{endpoint} , ServerInfo is not exist");
        //}

        var serverInfo = _serverInfoList.FirstOrDefault(e => e.IsValid() && e.GetBindEndpoint() == endpoint);

        if (serverInfo == null)
        {
            throw new CommunicatorException.NotExistServerInfo($"target endpoint:{endpoint} , ServerInfo is not exist");
        }

        return serverInfo;
    }

    public XServerInfo FindRoundRobinServer(ushort serviceId)
    {
        var list = _serverInfoList
            .Where(x => x.IsValid() && x.GetServiceId() == serviceId)
            .ToList();

        if (!list.Any())
        {
            throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
        }

        var next = Interlocked.Increment(ref _offset);
        if (next < 0)
        {
            next *= next * -1;
        }

        var index = next % list.Count;
        return list[index];
    }

    public IReadOnlyList<XServerInfo> GetServerList()
    {
        return _serverInfoList;
    }

    public XServerInfo FindServerByAccountId(ushort serviceId, long accountId)
    {
        var list = _serverInfoList
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
        var list = _serverInfoList
            .Where(info => info.GetServiceId() == serviceId)
            .ToList();

        if (list.Count == 0)
        {
            throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
        }

        return list.First().GetServiceType();
    }
}