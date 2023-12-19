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
        long GenerateUUID();
    }

    public interface ISender
    {
        ushort ServiceId { get; }
        void Reply(ReplyPacket reply);
        void SendToClient(string sessionEndpoint, int sid, IPacket packet);
        void SendToApi(string apiEndpoint, IPacket packet);
        void SendToStage(string playEndpoint, string stageId, string accountId, IPacket packet);

        void RequestToApi(string apiEndpoint, IPacket packet, ReplyCallback replyCallback);
        void RequestToStage(string playEndpoint, string stageId, string accountId, IPacket packet, ReplyCallback replyCallback);
        Task<ReplyPacket> RequestToApi(string apiEndpoint, IPacket packet);
        Task<ReplyPacket> RequestToStage(string playEndpoint, string stageId, string accountId, IPacket packet);

        TaskCompletionSource<ReplyPacket> AsyncToApi(string apiEndpoint, IPacket packet);
        TaskCompletionSource<ReplyPacket> AsyncToStage(string playEndpoint, string stageId, string accountId, IPacket packet);

        void SendToSystem(string endpoint, IPacket packet);
        Task<ReplyPacket> RequestToSystem(string endpoint, IPacket packet);

        void SessionClose(string sessionEndpoint, int sid);
    }

    public interface IApiCommonSender : ISender
    {

        string AccountId { get; }
        Task<CreateStageResult> CreateStage(string playEndpoint, string stageType, string stageId, IPacket packet);


    }
    public interface IApiSender : IApiCommonSender
    {
        void Authenticate(string accountId);
        string SessionEndpoint { get; }
        int Sid { get; }

        Task<JoinStageResult> JoinStage(string playEndpoint,
                    string stageId,
                    IPacket packet
      );
        Task<CreateJoinStageResult> CreateJoinStage(string playEndpoint,
                            string stageType,
                            string stageId,
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
        public string StageId { get; }
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
