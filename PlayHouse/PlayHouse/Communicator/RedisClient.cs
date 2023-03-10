using Playhouse.Protocol;
using StackExchange.Redis;

namespace PlayHouse.Communicator
{
    public  class RedisCache
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly string _redisKey = "playhouse_serverinfos";

        public RedisCache(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public void UpdateServerInfo(XServerInfo serverInfo)
        {
            var database = _connectionMultiplexer.GetDatabase();
            database.HashSet(_redisKey, serverInfo.BindEndpoint, serverInfo.ToByteArray());
        }

        public List<XServerInfo> GetServerList(string endpoint)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var hashEntries = database.HashGetAll(_redisKey);
            return hashEntries.Select(entry => XServerInfo.Of(ServerInfoMsg.Parser.ParseFrom(entry.Value)))
                              .Where(serverInfo => serverInfo.BindEndpoint != endpoint)
                              .ToList();
        }
    }
    public class RedisClient : IStorageClient
    {
        private string _redisURI = "";
        private RedisCache? _cache;

        public RedisClient(string redisIp, int redisBindPort)
        {
            _redisURI = $"redis://{redisIp}:{redisBindPort}";
        }

        public void Connect()
        {
            _cache = new RedisCache(ConnectionMultiplexer.Connect(_redisURI));
        }

        public void UpdateServerInfo(XServerInfo serverInfo)
        {
            _cache!.UpdateServerInfo(serverInfo);
        }

        public List<XServerInfo> GetServerList(string endpoint)
        {
            return _cache!.GetServerList(endpoint);
        }
    }
}
