using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Utils;
using PlayHouse.Service.Api;
using System.Threading.Tasks;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared;
internal class XSender : ISender
{
    private readonly LOG<XSender> _log = new();
    private readonly ushort _serviceId;
    private readonly IClientCommunicator _clientCommunicator;
    private readonly RequestCache _reqCache;

    protected RouteHeader? CurrentHeader;

    public XSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
    {
        _serviceId = serviceId;
        _clientCommunicator = clientCommunicator;
        _reqCache = reqCache;
    }

    public ushort ServiceId => _serviceId;

    public void SetCurrentPacketHeader(RouteHeader currentHeader)
    {
        CurrentHeader = currentHeader;
    }

    public void ClearCurrentPacketHeader()
    {
        CurrentHeader = null;
    }

    public void Reply(IPacket reply)
    {
        Reply((ushort)BaseErrorCode.Success, reply);
    }

    public void Reply(ushort errorCode)
    {
        Reply(errorCode, null);
    }

    private void Reply(ushort errorCode, IPacket? reply = null)
    {

        if (CurrentHeader != null)
        {
            ushort msgSeq = CurrentHeader.Header.MsgSeq;
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

                string from = CurrentHeader.From;
                RoutePacket routePacket = RoutePacket.ReplyOf(_serviceId, CurrentHeader, errorCode, reply);
                routePacket.RouteHeader.AccountId = CurrentHeader.AccountId;
                //_log.Trace(() => $"Before Send - [packetInfo:${routePacket.RouteHeader}]");
                _clientCommunicator.Send(from, routePacket);
            }
            else
            {
                if (reply != null)
                {
                    _log.Error(() => $"Not exist request packet - reply msgId:{reply.MsgId}, request msgId:{CurrentHeader.Header.MsgId}");
                }
                else
                {
                    _log.Error(() => $"Not exist request packet - reply errorCode:{errorCode}, request msgId:{CurrentHeader.Header.MsgId}");
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

    public void SendToClient(string sessionEndpoint, int sid, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Client, 0, packet);

        RoutePacket routePacket = RoutePacket.ClientOf(_serviceId, sid, packet);
        _clientCommunicator.Send(sessionEndpoint, routePacket);
    }

    public void SendToBaseSession(string sessionEndpoint, int sid, RoutePacket packet)
    {
        RoutePacket routePacket = RoutePacket.SessionOf(sid, packet, true, true);
        _clientCommunicator.Send(sessionEndpoint, routePacket);
    }

    public async Task<RoutePacket> RequestToBaseSession(string sessionEndpoint, int sid, RoutePacket packet)
    {
        ushort seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.SessionOf(sid, packet, true, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(sessionEndpoint, routePacket);

        RoutePacket reply = await deferred.Task;

        return reply;
        
    }

    private ushort GetSequence()
    {
        return _reqCache.GetSequence();
    }
    public void SendToApi(string apiEndpoint, string accountId, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Api, 0, packet);

        var routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), isBase: false, isBackend: true);
        routePacket.RouteHeader.AccountId = accountId;
        _clientCommunicator.Send(apiEndpoint, routePacket);
    }


    public void SendToApi(string apiEndpoint, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Api, 0, packet);

        RoutePacket routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        _clientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void RelayToApi(string apiEndpoint, int sid, RoutePacket packet, ushort msgSeq)
    {
        RoutePacket routePacket = RoutePacket.ApiOf(packet, false, false);
        routePacket.RouteHeader.Sid = sid;
        routePacket.RouteHeader.Header.MsgSeq = msgSeq;
        _clientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void SendToBaseApi(string apiEndpoint, string accountId, RoutePacket packet)
    {
        RoutePacket routePacket = RoutePacket.ApiOf(packet, true, true);
        routePacket.RouteHeader.AccountId = accountId;

        _clientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void SendToStage(string playEndpoint, string stageId, string accountId, IPacket packet)
    {
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, RoutePacket.Of(packet), false, true);
        _clientCommunicator.Send(playEndpoint, routePacket);
    }

    public void SendToBaseStage(string playEndpoint, string stageId, string accountId, RoutePacket packet)
    {
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
        _clientCommunicator.Send(playEndpoint, routePacket);
    }

    public async Task<RoutePacket> RequestToBaseApi(string apiEndpoint,RoutePacket request)    {

        ushort seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.ApiOf(request, true, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(apiEndpoint, routePacket);

        var replyPacket = await deferred.Task;
        ServiceAsyncContext.AddReply(replyPacket);

        return replyPacket;
    }

    public void RequestToApi(string apiEndpoint, IPacket packet, ReplyCallback replyCallback)
    {
        ushort seq = GetSequence();
        _reqCache.Put(seq, new ReplyObject(replyCallback));
        RoutePacket routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(apiEndpoint, routePacket);
    }

    public async Task<(ushort errorCode, IPacket reply)> RequestToApi(string apiEndpoint, IPacket packet)
    {
        RoutePacket replyPacket = await AsyncToApi(apiEndpoint, packet).Task;
        ServiceAsyncContext.AddReply(replyPacket);

        return (replyPacket.ErrorCode, CPacket.Of(replyPacket));
    }
    public async Task<(ushort errorCode, IPacket reply)> RequestToApi(string apiEndpoint, string accountId, IPacket packet)
    {
        return await AsyncToApi(apiEndpoint, accountId, packet);
    }
    public async Task<(ushort errorCode, IPacket reply)> AsyncToApi(string apiEndpoint, string accountId, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Api, 0, packet);

        ushort seq = GetSequence();
        var taskCompletionSource = new TaskCompletionSource<RoutePacket>();
        _reqCache.Put(seq, new ReplyObject(taskCompletionSource: taskCompletionSource));
        var routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        routePacket.RouteHeader.AccountId = accountId;
        _clientCommunicator.Send(apiEndpoint, routePacket);

        var replyPacket = await taskCompletionSource.Task;
        ServiceAsyncContext.AddReply(replyPacket);

        return (replyPacket.ErrorCode, CPacket.Of(replyPacket));
    }

    private TaskCompletionSource<RoutePacket> AsyncToApi(string apiEndpoint, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Api, 0, packet);

        ushort seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.ApiOf(RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(apiEndpoint, routePacket);
        return deferred;
    }

    public void RequestToStage(string playEndpoint, string stageId, string accountId, IPacket packet, ReplyCallback replyCallback)
    {
        PacketContext.AsyncCore.Add(SendTarget.Play, 0, packet);

        ushort seq = GetSequence();
        _reqCache.Put(seq, new ReplyObject(replyCallback));
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(playEndpoint, routePacket);
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

    private TaskCompletionSource<RoutePacket> AsyncToStage(string playEndpoint, string stageId, string accountId, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Play, 0, packet);

        ushort seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, RoutePacket.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(playEndpoint, routePacket);
        return deferred;
    }

    public async Task<(ushort errorCode, IPacket reply)> RequestToStage(string playEndpoint, string stageId, string accountId, IPacket packet)
    {
        RoutePacket replyPacket = await AsyncToStage(playEndpoint, stageId, accountId, packet).Task;
        ServiceAsyncContext.AddReply(replyPacket);

        return (replyPacket.ErrorCode, CPacket.Of(replyPacket));
    }

    public async Task<RoutePacket> RequestToBaseStage(string playEndpoint, string stageId, string accountId, RoutePacket packet)
    {
        ushort seq = GetSequence();
        var deferred = new TaskCompletionSource<RoutePacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(playEndpoint, routePacket);

        var replyPacket = await deferred.Task;
        ServiceAsyncContext.AddReply(replyPacket);

        return replyPacket;
    }

    public void SendToSystem(string endpoint, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.System, 0, packet);

        _clientCommunicator.Send(endpoint, RoutePacket.SystemOf(RoutePacket.Of(packet), false));
    }

    public async Task<(ushort errorCode, IPacket reply)> RequestToSystem(string endpoint, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.System, 0, packet);

        ushort msgSeq = GetSequence();
        RoutePacket routePacket = RoutePacket.SystemOf(RoutePacket.Of(packet), false);
        routePacket.RouteHeader.Header.MsgSeq = msgSeq;
        var deferred = new TaskCompletionSource<RoutePacket>();
        _reqCache.Put(msgSeq, new ReplyObject(null, deferred));
        _clientCommunicator.Send(endpoint, routePacket);
        var replyPacket = await deferred.Task;
        ServiceAsyncContext.AddReply(replyPacket);
        return (replyPacket.ErrorCode, CPacket.Of(replyPacket));
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
        SessionCloseMsg message = new SessionCloseMsg();
        SendToBaseSession(sessionEndpoint, sid, RoutePacket.Of(message));
    }


}
