using PlayHouse.Communicator;

namespace PlayHouse.Production.Shared;

internal interface IServerInfoCenter
{
    IReadOnlyList<XServerInfo> Update(IReadOnlyList<XServerInfo> serverList);
    XServerInfo FindServer(int nid);
    XServerInfo FindRoundRobinServer(ushort serviceId);
    IReadOnlyList<XServerInfo> GetServerList();
    XServerInfo FindServerByAccountId(ushort serviceId, long accountId);
    ServiceType FindServerType(ushort serviceId);
}