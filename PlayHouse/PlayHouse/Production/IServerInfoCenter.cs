using PlayHouse.Communicator;

namespace PlayHouse.Production
{
    internal interface IServerInfoCenter
    {
        IList<XServerInfo> Update(IList<XServerInfo> serverList);
        XServerInfo FindServer(string endpoint);
        XServerInfo FindRoundRobinServer(ushort serviceId);
        IList<XServerInfo> GetServerList();
        XServerInfo FindServerByAccountId(ushort serviceId, string accountId);
        ServiceType FindServerType(ushort serviceId);
    }
}
