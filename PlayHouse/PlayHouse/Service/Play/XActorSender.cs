using PlayHouse.Production.Shared;
using PlayHouse.Service.Play.Base;

namespace PlayHouse.Service.Play;

internal class XActorSender(
    long accountId,
    string sessionNid,
    long sid,
    string apiNid,
    BaseStage baseStage,
    IServerInfoCenter serverInfoCenter)
    : IActorSender
{
    public string SessionNid()
    {
        return sessionNid;
    }

    public string ApiNid()
    {
        return apiNid;
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
        var serverInfo = serverInfoCenter.FindServer(apiNid);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        baseStage.StageSender.SendToApi(serverInfo.GetNid(), accountId, packet);
    }

    public async Task<IPacket> RequestToApi(IPacket packet)
    {
        var serverInfo = serverInfoCenter.FindServer(apiNid);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        return await baseStage.StageSender.RequestToApi(serverInfo.GetNid(), accountId, packet);
    }

    public async Task<IPacket> AsyncToApi(IPacket packet)
    {
        var serverInfo = serverInfoCenter.FindServer(apiNid);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        return await baseStage.StageSender.AsyncToApi(serverInfo.GetNid(), accountId, packet);
    }

    public void Update(string sessionNetworkId, long sessionId, string apiNetworkWId)
    {
        sessionNid = sessionNetworkId;
        sid = sessionId;
        apiNid = apiNetworkWId;
    }
}