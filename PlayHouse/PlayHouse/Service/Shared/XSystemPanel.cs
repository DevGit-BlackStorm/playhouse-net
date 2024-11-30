using PlayHouse.Communicator;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared;

internal class XSystemPanel(
    IServerInfoCenter serverInfoCenter,
    IClientCommunicator clientCommunicator,
    int nid)
    : ISystemPanel
{
    private readonly IClientCommunicator _clientCommunicator = clientCommunicator;
    private readonly UniqueIdGenerator _uniqueIdGenerator = new(nid);

    public Communicator.Communicator? Communicator { get; set; }

    public IServerInfo GetServerInfoBy(ushort serviceId)
    {
        return serverInfoCenter.FindRoundRobinServer(serviceId);
    }

    public IServerInfo GetServerInfoBy(ushort serviceId, long accountId)
    {
        return serverInfoCenter.FindServerByAccountId(serviceId, accountId);
    }

    public IServerInfo GetServerInfoByEndpoint(int nid)
    {
        return serverInfoCenter.FindServer(nid);
    }

    public IList<IServerInfo> GetServers()
    {
        return serverInfoCenter.GetServerList().Cast<IServerInfo>().ToList();
    }

    public void Pause()
    {
        Communicator!.Pause();
    }

    public void Resume()
    {
        Communicator!.Resume();
    }

    public async Task ShutdownASync()
    {
        await Communicator!.StopAsync();
    }

    public ServerState GetServerState()
    {
        return Communicator!.GetServerState();
    }

    public long GenerateUUID()
    {
        return _uniqueIdGenerator.NextId();
    }

    public IServerInfo GetServerInfo()
    {
        return serverInfoCenter.FindServer(nid);
    }
}