using PlayHouse.Service.Shared;

namespace PlayHouse.Production.Shared;

public delegate Task TimerCallbackTask();

public interface ISystemPanel
{
    IServerInfo GetServerInfo();
    IServerInfo GetServerInfoBy(ushort serviceId);
    IServerInfo GetServerInfoBy(ushort serviceId, long accountId);
    IServerInfo GetServerInfoByEndpoint(string endpoint);
    IList<IServerInfo> GetServers();
    void Pause();
    void Resume();
    Task ShutdownASync();
    ServerState GetServerState();
    long GenerateUUID();
}

public interface ISender
{
    ushort ServiceId { get; }
    void Reply(ushort errorCode);
    void Reply(IPacket reply);

    void SendToClient(string sessionEndpoint, long sid, IPacket packet);

    void SendToApi(string apiEndpoint, IPacket packet);
    void SendToApi(string apiEndpoint, long accountId, IPacket packet);
    void SendToStage(string playEndpoint, long stageId, long accountId, IPacket packet);

    void RequestToApi(string apiEndpoint, IPacket packet, ReplyCallback replyCallback);
    void RequestToStage(string playEndpoint, long stageId, long accountId, IPacket packet, ReplyCallback replyCallback);
    Task<IPacket> RequestToApi(string apiEndpoint, IPacket packet);
    Task<IPacket> RequestToApi(string apiEndpoint, long accountId, IPacket packet);
    Task<IPacket> RequestToStage(string playEndpoint, long stageId, long accountId, IPacket packet);


    void SendToSystem(string endpoint, IPacket packet);
    Task<IPacket> RequestToSystem(string endpoint, IPacket packet);

    void SessionClose(string sessionEndpoint, long sid);
}

public interface IApiCommonSender : ISender
{
    long AccountId { get; }
    Task<CreateStageResult> CreateStage(string playEndpoint, string stageType, long stageId, IPacket packet);
}

public interface IApiSender : IApiCommonSender
{
    string SessionEndpoint { get; }
    long Sid { get; }
    void Authenticate(long accountId);

    Task<JoinStageResult> JoinStage(string playEndpoint,
        long stageId,
        IPacket packet
    );

    Task<CreateJoinStageResult> CreateJoinStage(string playEndpoint,
        string stageType,
        long stageId,
        IPacket createPacket,
        IPacket joinPacket
    );

    void SendToClient(IPacket packet)
    {
        SendToClient(SessionEndpoint, Sid, packet);
    }

    void SessionClose()
    {
        SessionClose(SessionEndpoint, Sid);
    }
}

public delegate Task<object> AsyncPreCallback();

public delegate Task AsyncPostCallback(object result);

public interface IStageSender : ISender
{
    public long StageId { get; }
    public string StageType { get; }

    long AddRepeatTimer(TimeSpan initialDelay, TimeSpan period, TimerCallbackTask timerCallback);
    long AddCountTimer(TimeSpan initialDelay, int count, TimeSpan period, TimerCallbackTask timerCallback);
    void CancelTimer(long timerId);
    void CloseStage();

    void AsyncBlock(AsyncPreCallback preCallback, AsyncPostCallback? postCallback = null);
}

public interface IApiBackendSender : IApiCommonSender
{
    string GetFromEndpoint();
}

public interface ISessionSender : ISender
{
}