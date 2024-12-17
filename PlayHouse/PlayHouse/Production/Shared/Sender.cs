using PlayHouse.Service.Shared;

namespace PlayHouse.Production.Shared;

public delegate Task TimerCallbackTask();

public interface ISender
{
    ushort ServiceId { get; }
    void Reply(ushort errorCode);
    void Reply(IPacket reply);

    void SendToClient(string sessionNid, long sid, IPacket packet);

    void SendToApi(string apiNid, IPacket packet);
    void SendToApi(string apiNid, long accountId, IPacket packet);
    void SendToStage(string playNid, long stageId, long accountId, IPacket packet);

    void RequestToApi(string apiNid, IPacket packet, ReplyCallback replyCallback);
    void RequestToStage(string playNid, long stageId, long accountId, IPacket packet, ReplyCallback replyCallback);
    Task<IPacket> RequestToApi(string apiNid, IPacket packet);
    Task<IPacket> RequestToApi(string apiNid, long accountId, IPacket packet);
    Task<IPacket> RequestToStage(string playNid, long stageId, long accountId, IPacket packet);


    void SendToSystem(string nid, IPacket packet);
    Task<IPacket> RequestToSystem(string nid, IPacket packet);

    void SessionClose(string sessionNid, long sid);
}

public interface IApiCommonSender : ISender
{
    long AccountId { get; }
    Task<CreateStageResult> CreateStage(string playNid, string stageType, long stageId, IPacket packet);
}

public interface IApiSender : IApiCommonSender
{
    string SessionNid { get; }
    long Sid { get; }
    void Authenticate(long accountId);
    void Authenticate(long accountId, string apiNid);

    Task<string> GetRemoteIp();

    Task<JoinStageResult> JoinStage(string playNid,
        long stageId,
        IPacket packet
    );

    Task<CreateJoinStageResult> CreateJoinStage(string playNid,
        string stageType,
        long stageId,
        IPacket createPacket,
        IPacket joinPacket
    );

    void SendToClient(IPacket packet)
    {
        SendToClient(SessionNid, Sid, packet);
    }

    void SessionClose()
    {
        SessionClose(SessionNid, Sid);
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
    string GetFromNid();
}

public interface ISessionSender : ISender
{
    void SendToClient(IPacket packet);
    void ReplyToClient(IPacket packet);
}