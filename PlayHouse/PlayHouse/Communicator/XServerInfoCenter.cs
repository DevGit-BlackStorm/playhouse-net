using System.Collections.Concurrent;
using Playhouse.Protocol;
using PlayHouse.Production;

namespace PlayHouse.Communicator
{
    public class XServerInfoCenter : IServerInfoCenter
    {
        private IDictionary<string, XServerInfo> _serverInfoMap = new ConcurrentDictionary<string, XServerInfo>();
        private List<XServerInfo> _serverInfoList = new List<XServerInfo>();
        private int _offset = 0;

        public XServerInfoCenter()
        {
        }

        public IList<XServerInfo> Update(IList<XServerInfo> serverList)
        {
            //var serverInfoMap = new ConcurrentDictionary<string, XServerInfo>();
            //foreach (XServerInfo serverInfo in serverList)
            //{
            //    serverInfo.CheckTimeout();
            //    _serverInfoMap.Add(serverInfo.BindEndpoint,serverInfo);
            //}
            //_serverInfoMap = serverInfoMap;
            //_serverInfoList = _serverInfoMap.Values.ToList().OrderBy(x => x.BindEndpoint).ToList();
            //return _serverInfoList;
            var updatedMap = new Dictionary<string, XServerInfo>();
            foreach (var newInfo in serverList)
            {
                newInfo.CheckTimeout();

                if (_serverInfoMap.TryGetValue(newInfo.BindEndpoint, out var oldInfo))
                {
                    if (oldInfo.Update(newInfo))
                    {
                        updatedMap[newInfo.BindEndpoint] = newInfo;
                    }
                }
                else
                {
                    _serverInfoMap[newInfo.BindEndpoint] = newInfo;
                    updatedMap[newInfo.BindEndpoint] = newInfo;
                }
            }

            // Remove server info if it's not in the list
            foreach (var oldInfo in _serverInfoMap.Values.ToList())
            {
                if (oldInfo.CheckTimeout())
                {
                    updatedMap[oldInfo.BindEndpoint] = oldInfo;
                }

                //if (!serverList.Contains(oldInfo))
                //{
                //    _serverInfoMap.Remove(oldInfo.BindEndpoint);
                //    if (oldInfo.CheckTimeout())
                //    {
                //        updatedMap[oldInfo.BindEndpoint] = oldInfo;
                //    }
                //}
            }

            _serverInfoList = _serverInfoMap.Values.ToList().OrderBy(x => x.BindEndpoint).ToList();

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

        public XServerInfo FindRoundRobinServer(short serviceId)
        {
            var list = _serverInfoList
                .Where(x => x.State == ServerState.RUNNING && x.ServiceId == serviceId)
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

        public XServerInfo FindServerByAccountId(short serviceId, long accountId)
        {
            var list = _serverInfoList
                .Where(info => info.State.Equals(ServerState.RUNNING) && info.ServiceId == serviceId)
                .ToList();

            if (list.Count == 0)
            {
                throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
            }

            int index = (int)(accountId % list.Count);
            return list[index];
        }

        public ServiceType FindServerType(short serviceId)
        {
            var list = _serverInfoList
                .Where(info => info.ServiceId == serviceId)
                .ToList();

            if (list.Count == 0)
            {
                throw new CommunicatorException.NotExistServerInfo($"serviceId:{serviceId} , ServerInfo is not exist");
            }

            return list.First().ServiceType;
        }
    }
}
