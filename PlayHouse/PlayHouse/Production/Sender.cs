using PlayHouse.Service;

namespace PlayHouse.Production
{
    public delegate Task TimerCallbackTask();
    public interface ISystemPanel
    {
        IServerInfo GetServerInfoByService(ushort serviceId);
        IServerInfo GetServerInfoByEndpoint(string endpoint);
        IList<IServerInfo> GetServers();
        void Pause();
        void Resume();
        void Shutdown();
        ServerState GetServerState();
        //long GenerateUUID();
    }

    public interface ISender
    {
        ushort ServiceId { get; }
        void Reply(ReplyPacket reply);
        void SendToClient(string sessionEndpoint, int sid, Packet packet);
        void SendToApi(string apiEndpoint, Packet packet);
        void SendToStage(string playEndpoint, Guid stageId, Guid accountId, Packet packet);

        void RequestToApi(string apiEndpoint, Packet packet, ReplyCallback replyCallback);
        void RequestToStage(string playEndpoint, Guid stageId, Guid accountId, Packet packet, ReplyCallback replyCallback);
        Task<ReplyPacket> RequestToApi(string apiEndpoint, Packet packet);
        Task<ReplyPacket> RequestToStage(string playEndpoint, Guid stageId, Guid accountId, Packet packet);

        TaskCompletionSource<ReplyPacket> AsyncToApi(string apiEndpoint, Packet packet);
        TaskCompletionSource<ReplyPacket> AsyncToStage(string playEndpoint, Guid stageId, Guid accountId, Packet packet);

        void SendToSystem(string endpoint, Packet packet);
        Task<ReplyPacket> RequestToSystem(string endpoint, Packet packet);

        void SessionClose(string sessionEndpoint, int sid);
    }

    public interface IApiCommonSender : ISender
    {

        Guid AccountId { get; }
        Task<CreateStageResult> CreateStage(string playEndpoint, string stageType, Guid stageId, Packet packet);


    }
    public interface IApiSender : IApiCommonSender
    {
        void Authenticate(Guid accountId);
        string SessionEndpoint { get; }
        int Sid { get; }

        Task<JoinStageResult> JoinStage(string playEndpoint,
                    Guid stageId,
                    Packet packet
      );
        Task<CreateJoinStageResult> CreateJoinStage(string playEndpoint,
                            string stageType,
                            Guid stageId,
                            Packet createPacket,
                            Packet joinPacket
        );

        void SendToClient(Packet packet)
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
        public Guid StageId { get; }
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

    public interface ISessionSender : ISender { }

}
