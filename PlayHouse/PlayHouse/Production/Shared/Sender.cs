using PlayHouse.Service.Shared;

namespace PlayHouse.Production.Shared;

public delegate Task TimerCallbackTask();

public interface ISender
{
    ushort ServiceId { get; }
    void Reply(ushort errorCode);
    void Reply(IPacket reply);

    void SendToClient(int sessionNid, long sid, IPacket packet);

    void SendToApi(int apiNid, IPacket packet);
    void SendToApi(int apiNid, long accountId, IPacket packet);
    void SendToStage(int playNid, long stageId, long accountId, IPacket packet);

    void RequestToApi(int apiNid, IPacket packet, ReplyCallback replyCallback);
    void RequestToStage(int playNid, long stageId, long accountId, IPacket packet, ReplyCallback replyCallback);
    Task<IPacket> RequestToApi(int apiNid, IPacket packet);
    Task<IPacket> RequestToApi(int apiNid, long accountId, IPacket packet);
    Task<IPacket> RequestToStage(int playNid, long stageId, long accountId, IPacket packet);


    void SendToSystem(int nid, IPacket packet);
    Task<IPacket> RequestToSystem(int nid, IPacket packet);

    void SessionClose(int sessionNid, long sid);
}

public interface IApiCommonSender : ISender
{
    long AccountId { get; }
    Task<CreateStageResult> CreateStage(int playNid, string stageType, long stageId, IPacket packet);
}

public interface IApiSender : IApiCommonSender
{
    int SessionNid { get; }
    long Sid { get; }
    Task AuthenticateAsync(long accountId);
    Task AuthenticateAsync(long accountId, int apiNid);

    Task<string> GetRemoteIp();

    Task<JoinStageResult> JoinStage(int playNid,
        long stageId,
        IPacket packet
    );

    Task<CreateJoinStageResult> CreateJoinStage(int playNid,
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
    int GetFromNid();
}

public interface ISessionSender : ISender
{
    void SendToClient(IPacket packet);
    void ReplyToClient(IPacket packet);
}