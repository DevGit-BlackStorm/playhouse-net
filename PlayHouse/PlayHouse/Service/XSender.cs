using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Production;
using PlayHouse.Utils;

namespace PlayHouse.Service;
public class XSender : ISender
{
    private readonly LOG<XSender> _log = new ();
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


    public void Reply(ReplyPacket reply)
    {
        if (CurrentHeader != null)
        {
            ushort msgSeq = CurrentHeader.Header.MsgSeq;
            if (msgSeq != 0)
            {
                //int sid = CurrentHeader.Sid;
                string from = CurrentHeader.From;
                //RoutePacket routePacket = RoutePacket.ReplyOf(_serviceId, msgSeq, sid, !CurrentHeader.IsBackend, reply);
                RoutePacket routePacket = RoutePacket.ReplyOf(_serviceId, CurrentHeader,  reply);
                //routePacket.RouteHeader.Sid = sid;
                //routePacket.RouteHeader.IsBase = CurrentHeader.IsBase;
                //routePacket.RouteHeader.IsBackend = CurrentHeader.IsBackend;
                
                //for test
                routePacket.RouteHeader.AccountId = CurrentHeader.AccountId;
                //_log.Trace(() => $"Before Send - [packetInfo:${routePacket.RouteHeader}]");
                
                _clientCommunicator.Send(from, routePacket);
            }
            else
            {
                _log.Error(()=>$"Not exist request packet - reply msgId:{reply.MsgId}, current msgId:{CurrentHeader.Header.MsgId}");
            }
        }
        else
        {
            _log.Error(()=>$"Not exist request packet {reply.MsgId}");
        }
    }

    public void SendToClient(string sessionEndpoint, int sid, IPacket packet)
    {
        RoutePacket routePacket = RoutePacket.ClientOf(_serviceId, sid, Packet.Of(packet));
        _clientCommunicator.Send(sessionEndpoint, routePacket);
    }

    public void SendToBaseSession(string sessionEndpoint, int sid, Packet packet)
    {
        RoutePacket routePacket = RoutePacket.SessionOf(sid, packet, true, true);
        _clientCommunicator.Send(sessionEndpoint, routePacket);
    }

    public async Task<ReplyPacket> RequestToBaseSession(string sessionEndpoint, int sid, Packet packet)
    {
        ushort seq = (ushort)GetSequence();
        var deferred = new TaskCompletionSource<ReplyPacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.SessionOf(sid, packet, true, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(sessionEndpoint, routePacket);

        return await deferred.Task;
    }

    private ushort GetSequence()
    {
        return _reqCache.GetSequence();
    }
    public void SendToApi(string apiEndpoint, string accountId, IPacket packet)
    {
        var routePacket = RoutePacket.ApiOf(Packet.Of(packet), isBase: false, isBackend: true);
        routePacket.RouteHeader.AccountId = accountId;
        _clientCommunicator.Send(apiEndpoint, routePacket);
    }



    public void SendToApi(string apiEndpoint, IPacket packet)
    {
        RoutePacket routePacket = RoutePacket.ApiOf(Packet.Of(packet), false, true);
        _clientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void RelayToApi(string apiEndpoint, int sid, Packet packet, ushort msgSeq)
    {
        RoutePacket routePacket = RoutePacket.ApiOf( packet, false, false);
        routePacket.RouteHeader.Sid = sid;
        routePacket.RouteHeader.Header.MsgSeq = msgSeq;
        _clientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void SendToBaseApi(string apiEndpoint, string accountId,Packet packet)
    {
        RoutePacket routePacket = RoutePacket.ApiOf( packet, true, true);
        routePacket.RouteHeader.AccountId = accountId;

        _clientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void SendToStage(string playEndpoint, string stageId, string accountId, IPacket packet)
    {
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, Packet.Of(packet), false, true);
        _clientCommunicator.Send(playEndpoint, routePacket);
    }

    public void SendToBaseStage(string playEndpoint, string stageId, string accountId, Packet packet)
    {
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
        _clientCommunicator.Send(playEndpoint, routePacket);
    }

    public void RequestToApi(string apiEndpoint, IPacket packet, ReplyCallback replyCallback)
    {
        ushort seq =  GetSequence();
        _reqCache.Put(seq, new ReplyObject(replyCallback));
        RoutePacket routePacket = RoutePacket.ApiOf(Packet.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(apiEndpoint, routePacket);
    }

    public async Task<ReplyPacket> RequestToApi(string apiEndpoint, IPacket packet)
    {
        return await AsyncToApi(apiEndpoint,  packet).Task;
    }
    public async Task<ReplyPacket> RequestToApi(string apiEndpoint, string accountId, IPacket packet)
    {
        return await AsyncToApi(apiEndpoint, accountId, packet);
    }
    public async Task<ReplyPacket> AsyncToApi(string apiEndpoint, string accountId, IPacket packet)
    {
        ushort seq = GetSequence();
        var taskCompletionSource = new TaskCompletionSource<ReplyPacket>();
        _reqCache.Put(seq, new ReplyObject(taskCompletionSource: taskCompletionSource));
        var routePacket = RoutePacket.ApiOf(Packet.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        routePacket.RouteHeader.AccountId = accountId;
        _clientCommunicator.Send(apiEndpoint, routePacket);
        return await taskCompletionSource.Task;
    }

    public TaskCompletionSource<ReplyPacket> AsyncToApi(string apiEndpoint, IPacket packet)
    {
        ushort seq = GetSequence();
        var deferred = new TaskCompletionSource<ReplyPacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.ApiOf(Packet.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(apiEndpoint, routePacket);
        return deferred;
    }

    public void RequestToStage(string playEndpoint, string stageId, string accountId, IPacket packet, ReplyCallback replyCallback)
    {
        ushort seq = GetSequence();
        _reqCache.Put(seq, new ReplyObject(replyCallback));
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, Packet.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(playEndpoint, routePacket);
    }

    public ReplyPacket CallToBaseRoom(string playEndpoint, string stageId, string accountId, Packet packet)
    {
        ushort seq = GetSequence();
        var future = new TaskCompletionSource<ReplyPacket>();
        _reqCache.Put(seq, new ReplyObject(null, future));
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(playEndpoint, routePacket);

        return future.Task.Result;
    }

    public TaskCompletionSource<ReplyPacket> AsyncToStage(string playEndpoint, string stageId, string accountId, IPacket packet)
    {
        ushort seq = GetSequence();
        var deferred = new TaskCompletionSource<ReplyPacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, Packet.Of(packet), false, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(playEndpoint, routePacket);
        return deferred;
    }

    public async Task<ReplyPacket> RequestToStage(string playEndpoint, string stageId, string accountId, IPacket packet)
    {
        return await AsyncToStage(playEndpoint, stageId, accountId, packet).Task;
    }

    public async Task<ReplyPacket> RequestToBaseStage(string playEndpoint, string stageId, string accountId, Packet packet)
    {
        ushort seq = GetSequence();
        var deferred = new TaskCompletionSource<ReplyPacket>();
        _reqCache.Put(seq, new ReplyObject(null, deferred));
        RoutePacket routePacket = RoutePacket.StageOf(stageId, accountId, packet, true, true);
        routePacket.SetMsgSeq(seq);
        _clientCommunicator.Send(playEndpoint, routePacket);
        return await deferred.Task;
    }

    public void SendToSystem(string endpoint, IPacket packet)
    {
        _clientCommunicator.Send(endpoint, RoutePacket.SystemOf(Packet.Of(packet), false));
    }

    public async Task<ReplyPacket> RequestToSystem(string endpoint, IPacket packet)
    {
        ushort msgSeq = GetSequence();
        RoutePacket routePacket = RoutePacket.SystemOf(Packet.Of(packet), false);
        routePacket.RouteHeader.Header.MsgSeq = msgSeq;
        var deferred = new TaskCompletionSource<ReplyPacket>();
        _reqCache.Put(msgSeq, new ReplyObject(null, deferred));
        _clientCommunicator.Send(endpoint, routePacket);
        return await deferred.Task;
    }

    public void ErrorReply(RouteHeader routeHeader, ushort errorCode)
    {
        ushort msgSeq = routeHeader.Header.MsgSeq;
        string from = routeHeader.From;
        //int sid = routeHeader.Sid;
        //bool forClient = routeHeader.IsToClient;
        if (msgSeq > 0)
        {
            //RoutePacket reply = RoutePacket.ReplyOf(_serviceId, msgSeq,sid,!routeHeader.IsBackend, new ReplyPacket(errorCode));
            RoutePacket reply = RoutePacket.ReplyOf(_serviceId, routeHeader,  new ReplyPacket(errorCode));
            _clientCommunicator.Send(from, reply);
        }
    }
    public void SessionClose(string sessionEndpoint, int sid)
    {
        SessionCloseMsg message = new SessionCloseMsg();
        SendToBaseSession(sessionEndpoint, sid, new Packet(message));
    }
}
