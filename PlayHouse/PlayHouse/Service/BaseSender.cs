﻿using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playhouse.Protocol;

namespace PlayHouse.Service
{
    public class BaseSender : ICommonSender
    {
        private readonly string _serviceId;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly RequestCache _reqCache;

        protected RouteHeader? _currentHeader;

        public BaseSender(string serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
        {
            _serviceId = serviceId;
            _clientCommunicator = clientCommunicator;
            _reqCache = reqCache;
        }

        public void SetCurrentPacketHeader(RouteHeader currentHeader)
        {
            _currentHeader = currentHeader;
        }

        public void ClearCurrentPacketHeader()
        {
            _currentHeader = null;
        }

        public string ServiceId()
        {
            return _serviceId;
        }

        public void Reply(ReplyPacket reply)
        {
            if (_currentHeader != null)
            {
                int msgSeq = _currentHeader.Header.MsgSeq;
                if (msgSeq != 0)
                {
                    int sid = _currentHeader.Sid;
                    string from = _currentHeader.From;
                    RoutePacket routePacket = RoutePacket.ReplyOf(_serviceId, msgSeq, reply);
                    routePacket.RouteHeader.Sid = sid;
                    routePacket.RouteHeader.ForClient = _currentHeader.ForClient;
                    _clientCommunicator.Send(from, routePacket);
                }
                else
                {
                    LOG.Error($"Not exist request packet {reply.MsgName}, {_currentHeader.Header.MsgName} is not request packet", GetType());
                }
            }
            else
            {
                LOG.Error($"Not exist request packet {reply.MsgName}", GetType());
            }
        }

        public void SendToClient(string sessionEndpoint, int sid, Packet packet)
        {
            RoutePacket routePacket = RoutePacket.ClientOf(_serviceId, sid, packet);
            _clientCommunicator.Send(sessionEndpoint, routePacket);
        }

        public void SendToBaseSession(string sessionEndpoint, int sid, Packet packet)
        {
            RoutePacket routePacket = RoutePacket.SessionOf(sid, packet, true, true);
            _clientCommunicator.Send(sessionEndpoint, routePacket);
        }

        public async Task<ReplyPacket> RequestToBaseSession(string sessionEndpoint, int sid, Packet packet)
        {
            int seq = GetSequence();
            var deferred = new TaskCompletionSource<ReplyPacket>();
            _reqCache.Put(seq, new ReplyObject(null, deferred));
            RoutePacket routePacket = RoutePacket.SessionOf(sid, packet, true, true);
            routePacket.SetMsgSeq(seq);
            _clientCommunicator.Send(sessionEndpoint, routePacket);

            return await deferred.Task;
        }

        private int GetSequence()
        {
            return _reqCache.GetSequence();
        }

        public void SendToApi(string apiEndpoint, string sessionInfo, Packet packet)
        {
            RoutePacket routePacket = RoutePacket.ApiOf(sessionInfo, packet, false, true);
            _clientCommunicator.Send(apiEndpoint, routePacket);
        }

        public void RelayToApi(string apiEndpoint, int sid, string sessionInfo, Packet packet, int msgSeq)
        {
            RoutePacket routePacket = RoutePacket.ApiOf(sessionInfo, packet, false, false);
            routePacket.RouteHeader.Sid = sid;
            routePacket.RouteHeader.Header.MsgSeq = msgSeq;
            _clientCommunicator.Send(apiEndpoint, routePacket);
        }

        public void SendToBaseApi(string apiEndpoint, string sessionInfo, Packet packet)
        {
            RoutePacket routePacket = RoutePacket.ApiOf(sessionInfo, packet, true, true);
            _clientCommunicator.Send(apiEndpoint, routePacket);
        }

        public void SendToStage(string playEndpoint, long stageId, long accountId, Packet packet)
        {
            RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, false, true);
            _clientCommunicator.Send(playEndpoint, routePacket);
        }

        public void SendToBaseStage(string playEndpoint, long stageId, long accountId, Packet packet)
        {
            RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
            _clientCommunicator.Send(playEndpoint, routePacket);
        }

        public void RequestToApi(string apiEndpoint, Packet packet, string sessionInfo, ReplyCallback replyCallback)
        {
            int seq = GetSequence();
            _reqCache.Put(seq, new ReplyObject(replyCallback));
            RoutePacket routePacket = RoutePacket.ApiOf(sessionInfo, packet, false, true);
            routePacket.SetMsgSeq(seq);
            _clientCommunicator.Send(apiEndpoint, routePacket);
        }

        public async Task<ReplyPacket> RequestToApi(string apiEndpoint, string sessionInfo, Packet packet)
        {
            return await AsyncToApi(apiEndpoint, sessionInfo, packet).Task;
        }

        public TaskCompletionSource<ReplyPacket> AsyncToApi(string apiEndpoint, string sessionInfo, Packet packet)
        {
            int seq = GetSequence();
            var deferred = new TaskCompletionSource<ReplyPacket>();
            _reqCache.Put(seq, new ReplyObject(null, deferred));
            RoutePacket routePacket = RoutePacket.ApiOf(sessionInfo, packet, false, true);
            routePacket.SetMsgSeq(seq);
            _clientCommunicator.Send(apiEndpoint, routePacket);
            return deferred;
        }

        public void RequestToRoom(string playEndpoint, long stageId, long accountId, Packet packet, ReplyCallback replyCallback)
        {
            int seq = GetSequence();
            _reqCache.Put(seq, new ReplyObject(replyCallback));
            RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, false, true);
            routePacket.SetMsgSeq(seq);
            _clientCommunicator.Send(playEndpoint, routePacket);
        }

        public ReplyPacket CallToBaseRoom(string playEndpoint, long stageId, long accountId, Packet packet)
        {
            int seq = GetSequence();
            var future = new TaskCompletionSource<ReplyPacket>();
            _reqCache.Put(seq, new ReplyObject(null, future));
            RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
            routePacket.SetMsgSeq(seq);
            _clientCommunicator.Send(playEndpoint, routePacket);

            return future.Task.Result;
        }

        public TaskCompletionSource<ReplyPacket> AsyncToStage(string playEndpoint, long stageId, long accountId, Packet packet)
        {
            int seq = GetSequence();
            var deferred = new TaskCompletionSource<ReplyPacket>();
            _reqCache.Put(seq, new ReplyObject(null, deferred));
            RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, false, true);
            routePacket.SetMsgSeq(seq);
            _clientCommunicator.Send(playEndpoint, routePacket);
            return deferred;
        }

        public async Task<ReplyPacket> RequestToStage(string playEndpoint, long stageId, long accountId, Packet packet)
        {
            return await AsyncToStage(playEndpoint, stageId, accountId, packet).Task;
        }

        public async Task<ReplyPacket> RequestToBaseRoom(string playEndpoint, long stageId, long accountId, Packet packet)
        {
            int seq = GetSequence();
            var deferred = new TaskCompletionSource<ReplyPacket>();
            _reqCache.Put(seq, new ReplyObject(null, deferred));
            RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
            routePacket.SetMsgSeq(seq);
            _clientCommunicator.Send(playEndpoint, routePacket);
            return await deferred.Task;
        }

        public void SendToSystem(string endpoint, Packet packet)
        {
            _clientCommunicator.Send(endpoint, RoutePacket.SystemOf(packet, false));
        }

        public async Task<ReplyPacket> RequestToSystem(string endpoint, Packet packet)
        {
            int msgSeq = _reqCache.GetSequence();
            RoutePacket routePacket = RoutePacket.SystemOf(packet, false);
            routePacket.RouteHeader.Header.MsgSeq = msgSeq;
            var deferred = new TaskCompletionSource<ReplyPacket>();
            _reqCache.Put(msgSeq, new ReplyObject(null, deferred));
            _clientCommunicator.Send(endpoint, routePacket);
            return await deferred.Task;
        }

        public void ErrorReply(RouteHeader routeHeader, int errorCode)
        {
            int msgSeq = routeHeader.Header.MsgSeq;
            string from = routeHeader.From;
            if (msgSeq > 0)
            {
                RoutePacket reply = RoutePacket.ReplyOf(_serviceId, msgSeq, new ReplyPacket(errorCode));
                _clientCommunicator.Send(from, reply);
            }
        }

        public void RelayToRoom(string playEndpoint, long stageId, int sid, long accountId, string sessionInfo, Packet packet, int msgSeq)
        {
            RoutePacket routePacket = RoutePacket.ApiOf(sessionInfo, packet, false, false);
            routePacket.RouteHeader.StageId = stageId;
            routePacket.RouteHeader.AccountId = accountId;
            routePacket.RouteHeader.Header.MsgSeq = msgSeq;
            routePacket.RouteHeader.Sid = sid;
            _clientCommunicator.Send(playEndpoint, routePacket);
        }

        public void SessionClose(string sessionEndpoint, int sid)
        {
            SessionCloseMsg message = new SessionCloseMsg();
            SendToBaseSession(sessionEndpoint, sid, new Packet(message));
        }
    }
}
