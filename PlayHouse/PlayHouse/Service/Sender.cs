using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    public interface ISystemPanel
    {
        IServerInfo RandomServerInfo(short serviceId);
        IServerInfo ServerInfo(string endpoint);
        IList<IServerInfo> ServerList();
        void Pause();
        void Resume();
        void Shutdown();
        ServerState ServerState();
    }

    public interface ICommonSender
    {
        short ServiceId();
        void Reply(ReplyPacket reply);
        void SendToClient(string sessionEndpoint, int sid, Packet packet);
        void SendToApi(string apiEndpoint, string sessionInfo, Packet packet);
        void SendToStage(string playEndpoint, long stageId, long accountId, Packet packet);

        void RequestToApi(string apiEndpoint, Packet packet, string sessionInfo, ReplyCallback replyCallback);
        void RequestToRoom(string playEndpoint, long stageId, long accountId, Packet packet, ReplyCallback replyCallback);
        Task<ReplyPacket> RequestToApi(string apiEndpoint, string sessionInfo, Packet packet);
        Task<ReplyPacket> RequestToStage(string playEndpoint, long stageId, long accountId, Packet packet);

        TaskCompletionSource<ReplyPacket> AsyncToApi(string apiEndpoint, string sessionInfo, Packet packet);
        TaskCompletionSource<ReplyPacket> AsyncToStage(string playEndpoint, long stageId, long accountId, Packet packet);

        void SendToSystem(string endpoint, Packet packet);
        Task<ReplyPacket> RequestToSystem(string endpoint, Packet packet);

        void SessionClose(string sessionEndpoint, int sid);
    }

    public interface IApiCommonSender : ICommonSender
    {
        void UpdateSession(string sessionEndpoint, int sid, short serviceId, string sessionInfo);

        CreateStageResult CreateStage(string playEndpoint, string stageType, Packet packet);
        JoinStageResult JoinStage(string playEndpoint,
                      long stageId,
                      long accountId,
                      string sessionEndpoint,
                      int sid,
                      Packet packet
        );
        CreateJoinStageResult CreateJoinStage(string playEndpoint, string stageType, long stageId,
                            Packet createPacket,
                            long accountId, string sessionEndpoint, int sid,
                            Packet joinPacket
        );

    }
    public interface IApiSender : IApiCommonSender
    {
        void Authenticate(long accountId, string sessionInfo);
        string SessionEndpoint();
        int Sid();

        string SessionInfo();
        void SendToClient(Packet packet);
        void SessionClose();

        void UpdateSession(short serviceId, string sessionInfo);
    }

    public delegate Task<T> AsyncPreCallback<T>();
    public delegate Task AsyncPostCallback<T>(T result);

    public interface IStageSender : ICommonSender
    {
        long StageId();
        string StageType();

        long AddRepeatTimer(TimeSpan initialDelay, TimeSpan period, TimerCallback timerCallback);
        long AddCountTimer(TimeSpan initialDelay, int count, TimeSpan period, TimerCallback timerCallback);
        void CancelTimer(long timerId);
        void CloseStage();

        Task<T> AsyncBlock<T>(AsyncPreCallback<T> preCallback, AsyncPostCallback<T>? postCallback = null);
    }

    public interface IApiBackendSender : IApiCommonSender
    {
        string GetFromEndpoint();
    }

    public interface ISessionSender : ICommonSender { }

}
