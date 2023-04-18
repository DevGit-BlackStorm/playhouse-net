﻿using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System;

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
        long GenerateUUID();
    }

    public interface ISender
    {
        short ServiceId();
        void Reply(ReplyPacket reply);
        void SendToClient(string sessionEndpoint, int sid, Packet packet);
        void SendToApi(string apiEndpoint, Packet packet);
        void SendToStage(string playEndpoint, long stageId, long accountId, Packet packet);

        void RequestToApi(string apiEndpoint, Packet packet, ReplyCallback replyCallback);
        void RequestToStage(string playEndpoint, long stageId, long accountId, Packet packet, ReplyCallback replyCallback);
        Task<ReplyPacket> RequestToApi(string apiEndpoint, Packet packet);
        Task<ReplyPacket> RequestToStage(string playEndpoint, long stageId, long accountId, Packet packet);

        TaskCompletionSource<ReplyPacket> AsyncToApi(string apiEndpoint, Packet packet);
        TaskCompletionSource<ReplyPacket> AsyncToStage(string playEndpoint, long stageId, long accountId, Packet packet);

        void SendToSystem(string endpoint, Packet packet);
        Task<ReplyPacket> RequestToSystem(string endpoint, Packet packet);

        void SessionClose(string sessionEndpoint, int sid);
    }

    public interface IApiCommonSender : ISender
    {

        long AccountId();
        Task<CreateStageResult> CreateStage(string playEndpoint, string stageType, long stageId, Packet packet);
        Task<JoinStageResult> JoinStage(string playEndpoint,
                      long stageId,
                      long accountId,
                      string sessionEndpoint,
                      int sid,
                      Packet packet
        );
        Task<CreateJoinStageResult> CreateJoinStage(string playEndpoint, string stageType, long stageId,
                            Packet createPacket,
                            long accountId, string sessionEndpoint, int sid,
                            Packet joinPacket
        );

    }
    public interface IApiSender : IApiCommonSender
    {
        void Authenticate(long accountId);
        string SessionEndpoint();
        int Sid();

        void SendToClient(Packet packet)
        {
            SendToClient(SessionEndpoint(), Sid(),packet);
        }
        void SessionClose()
        {
            SessionClose(SessionEndpoint(), Sid());
        }

    }

    public delegate Task<T> AsyncPreCallback<T>();
    public delegate Task AsyncPostCallback<T>(T result);

    public interface IStageSender : ISender
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

    public interface ISessionSender : ISender { }

}
