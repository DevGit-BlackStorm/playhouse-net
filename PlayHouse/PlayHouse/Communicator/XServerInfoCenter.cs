using System.Collections.Concurrent;

namespace PlayHouse.Communicator
{
    public class XServerInfoCenter : IServerInfoCenter
    {
        private readonly IDictionary<string, XServerInfo> _serverInfoMap = new ConcurrentDictionary<string, XServerInfo>();
        private List<XServerInfo> _serverInfoList = new List<XServerInfo>();
        private int _offset = 0;

        public XServerInfoCenter()
        {
        }

        public IList<XServerInfo> Update(IList<XServerInfo> serverList)
        {
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
                if (!serverList.Contains(oldInfo))
                {
                    _serverInfoMap.Remove(oldInfo.BindEndpoint);
                    if (oldInfo.CheckTimeout())
                    {
                        updatedMap[oldInfo.BindEndpoint] = oldInfo;
                    }
                }
            }

            _serverInfoList = _serverInfoMap.Values.ToList().OrderBy(x => x.BindEndpoint).ToList();

            return updatedMap.Values.ToList();
        }

        public XServerInfo FindServer(string endpoint)
        {
            if (!_serverInfoMap.TryGetValue(endpoint, out var serverInfo) || !serverInfo.IsValid())
            {
                throw new CommunicatorException.NotExistServerInfo();
            }

            return serverInfo;
        }

        public XServerInfo FindRoundRobinServer(string serviceId)
        {
            var list = _serverInfoList
                .Where(x => x.State == ServerState.RUNNING && x.ServiceId == serviceId)
            .ToList();

            if (!list.Any())
            {
                throw new CommunicatorException.NotExistServerInfo();
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
    }
}
