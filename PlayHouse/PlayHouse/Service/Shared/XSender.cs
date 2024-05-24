using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;
using PlayHouse.Utils;

namespace PlayHouse.Service.Shared;

internal class XSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
    : ISender
{
    protected readonly IClientCommunicator ClientCommunicator = clientCommunicator;
    private readonly LOG<XSender> _log = new();
    private readonly ushort _serviceId = serviceId;

    protected RouteHeader? CurrentHeader;

    public ushort ServiceId => _serviceId;

    public void Reply(IPacket reply)
    {
        Reply((ushort)BaseErrorCode.Success, reply);
    }

    public void Reply(ushort errorCode)
    {
        Reply(errorCode, null);
    }

    public virtual void SendToClient(string sessionEndpoint, int sid, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Client, 0, packet);

        var routePacket = RoutePacket.ClientOf(_serviceId, sid, packet);
        ClientCommunicator.Send(sessionEndpoint, routePacket);
    }

    public void SendToApi(string apiEndpoint, long accountId, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Api, 0, packet);

        var routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        routePacket.RouteHeader.AccountId = accountId;
        ClientCommunicator.Send(apiEndpoint, routePacket);
    }


    public void SendToApi(string apiEndpoint, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Api, 0, packet);

        var routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        ClientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void SendToStage(string playEndpoint, long stageId, long accountId, IPacket packet)
    {
        var routePacket = RoutePacket.StageOf(stageId, accountId, RoutePacket.Of(packet), false, true);
        ClientCommunicator.Send(playEndpoint, routePacket);
    }

    public void RequestToApi(string apiEndpoint, IPacket packet, ReplyCallback replyCallback)
    {
        var seq = GetSequence();
        reqCache.Put(seq, new ReplyObject(replyCallback));
        var routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        ClientCommunicator.Send(apiEndpoint, routePacket);
    }

    public async Task<IPacket> RequestToApi(string apiEndpoint, IPacket packet)
    {
        var replyPacket = await AsyncToApi(apiEndpoint, packet).Task;
        //ServiceAsyncContext.AddReply(replyPacket);

        return CPacket.Of(replyPacket);
    }

    public async Task<IPacket> RequestToApi(string apiEndpoint, long accountId, IPacket packet)
    {
        return await AsyncToApi(apiEndpoint, accountId, packet);
    }

    public void RequestToStage(string playEndpoint, long stageId, long accountId, IPacket packet,
        ReplyCallback replyCallback)
    {
        PacketContext.AsyncCore.Add(SendTarget.Play, 0, packet);

        var seq = GetSequence();
        reqCache.Put(seq, new ReplyObject(replyCallback));
        var routePacket = RoutePacket.StageOf(stageId, accountId, RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        ClientCommunicator.Send(playEndpoint, routePacket);
    }

    public async Task<IPacket> RequestToStage(string playEndpoint, long stageId, long accountId, IPacket packet)
    {
        var replyPacket = await AsyncToStage(playEndpoint, stageId, accountId, packet).Task;
        //ServiceAsyncContext.AddReply(replyPacket);

        return CPacket.Of(replyPacket);
    }

    public void SendToSystem(string endpoint, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.System, 0, packet);

        ClientCommunicator.Send(endpoint, RoutePacket.SystemOf(RoutePacket.Of(packet), false));
    }

    public async Task<IPacket> RequestToSystem(string endpoint, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.System, 0, packet);

        var msgSeq = GetSequence();
        var routePacket = RoutePacket.SystemOf(RoutePacket.Of(packet), false);
        routePacket.RouteHeader.Header.MsgSeq = msgSeq;
        var deferred = new TaskCompletionSource<RoutePacket>();
        reqCache.Put(msgSeq, new ReplyObject(null, deferred));
        ClientCommunicator.Send(endpoint, routePacket);
        var replyPacket = await deferred.Task;
        //ServiceAsyncContext.AddReply(replyPacket);
        return CPacket.Of(replyPacket);
    }

    //public void ErrorReply(RouteHeader routeHeader, ushort errorCode)
    //{
    //    ushort msgSeq = routeHeader.Header.MsgSeq;
    //    string from = routeHeader.From;


    //    PacketContext.AsyncCore.Add(SendTarget.ErrorReply, CPacket.OfEmpty(msgSeq));


    //    //int sid = routeHeader.Sid;
    //    //bool forClient = routeHeader.IsToClient;
    //    if (msgSeq > 0)
    //    {
    //        //RoutePacket reply = RoutePacket.ReplyOf(GetServiceId, msgSeq,sid,!routeHeader.IsBackend, new ReplyPacket(errorCode));
    //        RoutePacket reply = RoutePacket.ReplyOf(GetServiceId, routeHeader, errorCode,  CPacket.OfEmpty(msgSeq));
    //        _clientCommunicator.Send(from, reply);
    //    }
    //}
    public void SessionClose(string sessionEndpoint, int sid)
    {
        var message = new SessionCloseMsg();
        SendToBaseSession(sessionEndpoint, sid, RoutePacket.Of(message));
    }

    public void SetCurrentPacketHeader(RouteHeader currentHeader)
    {
        CurrentHeader = currentHeader;
    }

    public void ClearCurrentPacketHeader()
    {
        CurrentHeader = null;
    }

    private void Reply(ushort errorCode, IPacket? reply = null)
    {
        if (CurrentHeader != null)
        {
            var msgSeq = CurrentHeader.Header.MsgSeq;
            if (msgSeq != 0)
            {
                if (reply != null)
                {
                    PacketContext.AsyncCore.Add(SendTarget.Reply, msgSeq, reply);
                }
                else
                {
                    PacketContext.AsyncCore.Add(SendTarget.ErrorReply, msgSeq, errorCode);
                }

                var from = CurrentHeader.From;
                var routePacket = RoutePacket.ReplyOf(_serviceId, CurrentHeader, errorCode, reply);
                routePacket.RouteHeader.AccountId = CurrentHeader.AccountId;
                //_log.Trace(() => $"Before Send - [packetInfo:${routePacket.RouteHeader}]");
                ClientCommunicator.Send(from, routePacket);
            }
            else
            {
                if (reply != null)
                {
                    _log.Error(() =>
                        $"Not exist request packet - reply msgId:{reply.MsgId}, request msgId:{CurrentHeader.Header.MsgId}");
                }
                else
                {
                    _log.Error(() =>
                        $"Not exist request packet - reply errorCode:{errorCode}, request msgId:{CurrentHeader.Header.MsgId}");
                }
            }
        }
        else
        {
            if (reply != null)
            {
                _log.Error(() => $"Not exist request packet - [reply msgId:{reply.MsgId}]");
            }
            else
            {
                _log.Error(() => $"Not exist request packet - [reply errorCode:{errorCode}]");
            }
        }
    }

    public void SendToBaseSession(string sessionEndpoint, int sid, RoutePacket packet)
    {
        var routePacket = RoutePacket.SessionOf(sid, packet, true, true);
        ClientCommunicator.Send(sessionEndpoint, routePacket);
    }

    public async Task<RoutePacket> RequestToBaseSession(string sessionEndpoint, int sid, RoutePacket packet)
    {
        var seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        reqCache.Put(seq, new ReplyObject(null, deferred));
        var routePacket = RoutePacket.SessionOf(sid, packet, true, true);
        routePacket.SetMsgSeq(seq);
        ClientCommunicator.Send(sessionEndpoint, routePacket);

        var reply = await deferred.Task;

        return reply;
    }

    private ushort GetSequence()
    {
        return reqCache.GetSequence();
    }

    public void RelayToApi(string apiEndpoint, int sid, RoutePacket packet, ushort msgSeq)
    {
        var routePacket = RoutePacket.ApiOf(packet, false, false);
        routePacket.RouteHeader.Sid = sid;
        routePacket.RouteHeader.Header.MsgSeq = msgSeq;
        ClientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void SendToBaseApi(string apiEndpoint, long accountId, RoutePacket packet)
    {
        var routePacket = RoutePacket.ApiOf(packet, true, true);
        routePacket.RouteHeader.AccountId = accountId;

        ClientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void SendToBaseStage(string playEndpoint, long stageId, long accountId, RoutePacket packet)
    {
        var routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
        ClientCommunicator.Send(playEndpoint, routePacket);
    }

    public async Task<RoutePacket> RequestToBaseApi(string apiEndpoint, RoutePacket request)
    {
        var seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        reqCache.Put(seq, new ReplyObject(null, deferred));
        var routePacket = RoutePacket.ApiOf(request, true, true);
        routePacket.SetMsgSeq(seq);
        ClientCommunicator.Send(apiEndpoint, routePacket);

        var replyPacket = await deferred.Task;
        //ServiceAsyncContext.AddReply(replyPacket);

        return replyPacket;
    }

    public async Task<IPacket> AsyncToApi(string apiEndpoint, long accountId, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Api, 0, packet);

        var seq = GetSequence();
        var taskCompletionSource = new TaskCompletionSource<RoutePacket>();
        reqCache.Put(seq, new ReplyObject(taskCompletionSource: taskCompletionSource));
        var routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        routePacket.RouteHeader.AccountId = accountId;
        ClientCommunicator.Send(apiEndpoint, routePacket);

        var replyPacket = await taskCompletionSource.Task;
        //ServiceAsyncContext.AddReply(replyPacket);

        return CPacket.Of(replyPacket);
    }

    private TaskCompletionSource<RoutePacket> AsyncToApi(string apiEndpoint, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Api, 0, packet);

        var seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        reqCache.Put(seq, new ReplyObject(null, deferred));
        var routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        ClientCommunicator.Send(apiEndpoint, routePacket);
        return deferred;
    }

    //public ReplyPacket CallToBaseRoom(string playEndpoint, string stageId, string accountId, Packet packet)
    //{
    //    ushort seq = GetSequence();
    //    var future = new TaskCompletionSource<ReplyPacket>();
    //    _reqCache.Put(seq, new ReplyObject(null, future));
    //    RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
    //    routePacket.SetMsgSeq(seq);
    //    _clientCommunicator.Send(playEndpoint, routePacket);

    //    return future.Task.Result;
    //}

    private TaskCompletionSource<RoutePacket> AsyncToStage(string playEndpoint, long stageId, long accountId,
        IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Play, 0, packet);

        var seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        reqCache.Put(seq, new ReplyObject(null, deferred));
        var routePacket = RoutePacket.StageOf(stageId, accountId, RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        ClientCommunicator.Send(playEndpoint, routePacket);
        return deferred;
    }

    public async Task<RoutePacket> RequestToBaseStage(string playEndpoint, long stageId, long accountId,
        RoutePacket packet)
    {
        var seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        reqCache.Put(seq, new ReplyObject(null, deferred));
        var routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
        routePacket.SetMsgSeq(seq);
        ClientCommunicator.Send(playEndpoint, routePacket);

        var replyPacket = await deferred.Task;
        //ServiceAsyncContext.AddReply(replyPacket);

        return replyPacket;
    }
}