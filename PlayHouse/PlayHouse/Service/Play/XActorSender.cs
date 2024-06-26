using PlayHouse.Production.Shared;
using PlayHouse.Service.Play.Base;

namespace PlayHouse.Service.Play;

internal class XActorSender(
    long accountId,
    string sessionEndpoint,
    long sid,
    string endpoint,
    BaseStage baseStage,
    IServerInfoCenter serverInfoCenter)
    : IActorSender
{
    public string SessionEndpoint()
    {
        return sessionEndpoint;
    }

    public string ApiEndpoint()
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
        baseStage.LeaveStage(accountId, sessionEndpoint, sid);
    }

    public void SendToClient(IPacket packet)
    {
        baseStage.StageSender.SendToClient(sessionEndpoint, sid, packet);
    }

    public void SendToApi(IPacket packet)
    {
        var serverInfo = serverInfoCenter.FindServer(endpoint);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        baseStage.StageSender.SendToApi(serverInfo.GetBindEndpoint(), accountId, packet);
    }

    public async Task<IPacket> RequestToApi(IPacket packet)
    {
        var serverInfo = serverInfoCenter.FindServer(endpoint);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        return await baseStage.StageSender.RequestToApi(serverInfo.GetBindEndpoint(), accountId, packet);
    }

    public async Task<IPacket> AsyncToApi(IPacket packet)
    {
        var serverInfo = serverInfoCenter.FindServer(endpoint);
        if (!serverInfo.IsValid())
        {
            serverInfo = serverInfoCenter.FindServerByAccountId(serverInfo.GetServiceId(), accountId);
        }

        return await baseStage.StageSender.AsyncToApi(serverInfo.GetBindEndpoint(), accountId, packet);
    }

    public void Update(string sessionEndpoint1, long sid1, string apiEndpoint)
    {
        sessionEndpoint = sessionEndpoint1;
        sid = sid1;
        endpoint = apiEndpoint;
    }
}