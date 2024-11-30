using PlayHouse.Production.Shared;
using PlayHouse.Service.Play.Base;

namespace PlayHouse.Service.Play;

internal class XActorSender(
    long accountId,
    int sessionNid,
    long sid,
    int endpoint,
    BaseStage baseStage,
    IServerInfoCenter serverInfoCenter)
    : IActorSender
{
    public int SessionNid()
    {
        return sessionNid;
    }

    public int ApiNid()
    {
        return endpoint;
    }

    public long Sid()
    {
        return sid;
    }

    public long AccountId()
    {
        return accountId;
    }

    public void LeaveStage()
    {
        baseStage.LeaveStage(accountId, sessionNid, sid);
    }

    public void SendToClient(IPacket packet)
    {
        baseStage.StageSender.SendToClient(sessionNid, sid, packet);
    }

    public void SendToApi(IPacket packet)
    {
        var serverInfo = serverInfoCenter.FindServer(endpoint);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        baseStage.StageSender.SendToApi(serverInfo.GetNid(), accountId, packet);
    }

    public async Task<IPacket> RequestToApi(IPacket packet)
    {
        var serverInfo = serverInfoCenter.FindServer(endpoint);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        return await baseStage.StageSender.RequestToApi(serverInfo.GetNid(), accountId, packet);
    }

    public async Task<IPacket> AsyncToApi(IPacket packet)
    {
        var serverInfo = serverInfoCenter.FindServer(endpoint);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        return await baseStage.StageSender.AsyncToApi(serverInfo.GetNid(), accountId, packet);
    }

    public void Update(int sessionNetworkId, long sid1, int apiNid)
    {
        sessionNid = sessionNetworkId;
        sid = sid1;
        endpoint = apiNid;
    }
}