using CommonLib;
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
        private readonly string _nodeIdeKey = "playhouse_nodeId";
        private readonly string _nodeSequenceKey = "playhouse_nodeId_seq";
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

        public int GetNodeId(string bindEndpoint)
        {
            byte[] key = System.Text.Encoding.UTF8.GetBytes(bindEndpoint);
            byte[] nodeIdBytes = _database!.HashGet(_nodeIdeKey, key)!;

            if (nodeIdBytes != null && nodeIdBytes.Length > 0)
            {
                return BitConverter.ToInt32(nodeIdBytes, 0);
            }
            else
            {
                int nodeId = (int)_database.StringIncrement(_nodeSequenceKey);

                if (nodeId > 4095)
                {
                    throw new ArgumentException("Node ID value exceeds maximum value");
                }
                _database!.HashSet(_nodeIdeKey,key,BitConverter.GetBytes(nodeId));


                return nodeId;
            }
        }
    }
}
