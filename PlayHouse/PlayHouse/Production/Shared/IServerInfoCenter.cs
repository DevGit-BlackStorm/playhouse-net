using PlayHouse.Communicator;

namespace PlayHouse.Production.Shared;

internal interface IServerInfoCenter
{
    IReadOnlyList<XServerInfo> Update(IReadOnlyList<XServerInfo> serverList);
    XServerInfo FindServer(string endpoint);
    XServerInfo FindRoundRobinServer(ushort serviceId);
    IReadOnlyList<XServerInfo> GetServerList();
    XServerInfo FindServerByAccountId(ushort serviceId, long accountId);
    ServiceType FindServerType(ushort serviceId);
}