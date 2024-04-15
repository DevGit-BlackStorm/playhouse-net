using System.Collections.Concurrent;
using PlayHouse.Production.Shared;

namespace PlayHouse.Communicator
{
    internal class XServerInfoCenter : IServerInfoCenter
    {
        private readonly IDictionary<string, XServerInfo> _serverInfoMap = new ConcurrentDictionary<string, XServerInfo>();
        private List<XServerInfo> _serverInfoList = new();
        private int _offset;

        public XServerInfoCenter()
        {
        }

        public IList<XServerInfo> Update(IList<XServerInfo> serverList)
        {
            //var serverInfoMap = new ConcurrentDictionary<string, XServerInfo>();
            //foreach (XServerInfo serverInfo in serverList)
            //{
            //    serverInfo.CheckTimeout();
            //    _serverInfoMap.Add(serverInfo.GetBindEndpoint,serverInfo);
            //}
            //_serverInfoMap = serverInfoMap;
            //_serverInfoList = _serverInfoMap.Values.ToList().OrderBy(x => x.GetBindEndpoint).ToList();
            //return _serverInfoList;
            var updatedMap = new Dictionary<string, XServerInfo>();
            foreach (var newInfo in serverList)
            {
                newInfo.CheckTimeout();

                if (_serverInfoMap.TryGetValue(newInfo.GetBindEndpoint(), out var oldInfo))
                {
                    if (oldInfo.Update(newInfo))
                    {
                        updatedMap[newInfo.GetBindEndpoint()] = newInfo;
                    }
                }
                else
                {
                    _serverInfoMap[newInfo.GetBindEndpoint()] = newInfo;
                    updatedMap[newInfo.GetBindEndpoint()] = newInfo;
                }
            }

            // Remove server info if it's not in the list
            foreach (var oldInfo in _serverInfoMap.Values.ToList())
            {
                if (oldInfo.CheckTimeout())
                {
                    updatedMap[oldInfo.GetBindEndpoint()] = oldInfo;
                }

                //if (!serverList.Contains(oldInfo))
                //{
                //    _serverInfoMap.Remove(oldInfo.GetBindEndpoint);
                //    if (oldInfo.CheckTimeout())
                //    {
                //        updatedMap[oldInfo.GetBindEndpoint] = oldInfo;
                //    }
                //}
            }

            _serverInfoList = _serverInfoMap.Values.ToList().OrderBy(x => x.GetBindEndpoint()).ToList();

            return updatedMap.Values.ToList();
        }

        public XServerInfo FindServer(string endpoint)
        {
            if (!_serverInfoMap.TryGetValue(endpoint, out var serverInfo) || !serverInfo.IsValid())
            {
                throw new CommunicatorException.NotExistServerInfo($"target endpoint:{endpoint} , ServerInfo is not exist");
            }

            return serverInfo;
        }

        public XServerInfo FindRoundRobinServer(ushort serviceId)
        {
            var list = _serverInfoList
                .Where(x => x.GetState() == ServerState.RUNNING && x.GetServiceId() == serviceId)
            .ToList();

            if (!list.Any())
            {
                throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
            }

            var next = Interlocked.Increment(ref _offset);
            if (next < 0)
            {
                next *= next * (-1);
            }

            var index = next % list.Count;
            return list[index];
        }

        public IList<XServerInfo> GetServerList()
        {
            return _serverInfoList;
        }

        public XServerInfo FindServerByAccountId(ushort serviceId, long accountId)
        {
            var list = _serverInfoList
                .Where(info => info.GetState() == ServerState.RUNNING && info.GetServiceId() == serviceId)
                .ToList();

            if (list.Count == 0)
            {
                throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
            }

            int index = (int)(accountId % list.Count);
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
}
