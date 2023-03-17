using Playhouse.Protocol;
using StackExchange.Redis;

namespace PlayHouse.Communicator
{
    //public  class RedisCache
    //{
    //    private readonly IConnectionMultiplexer _connectionMultiplexer;
    //    private readonly string _redisKey = "playhouse_serverinfos";

    //    public RedisCache(IConnectionMultiplexer connectionMultiplexer)
    //    {
    //        _connectionMultiplexer = connectionMultiplexer;
    //    }

    //    public void UpdateServerInfo(XServerInfo serverInfo)
    //    {
    //        var database = _connectionMultiplexer.GetDatabase();
    //        database.HashSet(_redisKey, serverInfo.BindEndpoint, serverInfo.ToByteArray());
    //    }

    //    public List<XServerInfo> GetServerList(string endpoint)
    //    {
    //        var database = _connectionMultiplexer.GetDatabase();
    //        var hashEntries = database.HashGetAll(_redisKey);
    //        return hashEntries.Select(entry => XServerInfo.Of(ServerInfoMsg.Parser.ParseFrom(entry.Value)))
    //                          .Where(serverInfo => serverInfo.BindEndpoint != endpoint)
    //                          .ToList();
    //    }
    //}
    public class RedisStorageClient : IStorageClient
    {
        private string _redisURI = "";
        private IConnectionMultiplexer? _connectionMultiplexer;
        private readonly string _redisKey = "playhouse_serverinfos";
        private IDatabase? _database ;

        public RedisStorageClient(string redisIp, int redisBindPort)
        {
            _redisURI = $"{redisIp}:{redisBindPort}";
        }

        public void Connect()
        {
            _connectionMultiplexer = ConnectionMultiplexer.Connect(_redisURI);
            _database = _connectionMultiplexer.GetDatabase();
        }

        public void UpdateServerInfo(XServerInfo serverInfo)
        {            
            _database!.HashSet(_redisKey, serverInfo.BindEndpoint, serverInfo.ToByteArray());
        }

        public List<XServerInfo> GetServerList(string endpoint)
        {
            var hashEntries = _database!.HashGetAll(_redisKey);
            return hashEntries.Select(entry => XServerInfo.Of(ServerInfoMsg.Parser.ParseFrom(entry.Value)))
                              .Where(serverInfo => serverInfo.BindEndpoint != endpoint)
                              .ToList();
        }
    }
}
