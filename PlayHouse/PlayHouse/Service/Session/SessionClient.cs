using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Service.Session.Network;
using PlayHouse.Utils;
using System.Collections.Concurrent;
using PlayHouse.Production;
using CommonLib;
using NetMQ;

namespace PlayHouse.Service.Session;
internal class TargetAddress
{
    public string Endpoint { get; }
    public string StageId { get; }

    public TargetAddress(string endpoint, string? stageId = null)
    {
        Endpoint = endpoint;
        StageId = stageId ?? string.Empty;
    }
}
internal class StageIndexGenerator
{
    private byte _byteValue;

    public byte IncrementByte()
    {
        _byteValue = (byte)((_byteValue + 1) & 0xff);
        if(_byteValue == 0)
        {
            _byteValue = IncrementByte();
        }
        return _byteValue;
    }
}

internal class SessionClient
{
    private readonly LOG<SessionClient> _log = new ();
    private readonly int _sid;
    private readonly IServerInfoCenter _serviceInfoCenter;
    private readonly ISession _session;

    private readonly XSessionSender _sessionSender;
    private readonly TargetServiceCache _targetServiceCache;
    private readonly ConcurrentQueue<RoutePacket> _msgQueue = new();
    private readonly AtomicBoolean _isUsing = new(false);

    public  bool IsAuthenticated { get; private set; }
    private readonly HashSet<string> _signInUrIs = new();
    private string _accountId = string.Empty;

    private readonly Dictionary<int, TargetAddress> _playEndpoints = new();
    private ushort _authenticateServiceId;
    private string _authServerEndpoint = "";
    private readonly StageIndexGenerator _stageIndexGenerator = new();

    public SessionClient(
        ushort serviceId, 
        int sid, 
        IServerInfoCenter serviceInfoCenter, 
        ISession session ,
        IClientCommunicator clientCommunicator, 
        List<string> urls, 
        RequestCache reqCache
        )
    {
        _sid = sid;
        _serviceInfoCenter = serviceInfoCenter;
        _session = session;

        _sessionSender = new XSessionSender(serviceId, clientCommunicator, reqCache);
        _targetServiceCache = new TargetServiceCache(serviceInfoCenter);

        _signInUrIs.UnionWith(urls);
    }


    private void Authenticate(ushort serviceId, string apiEndpoint, string accountId)
    {
        _accountId = accountId;
        IsAuthenticated = true;
        _authenticateServiceId = serviceId;
        _authServerEndpoint = apiEndpoint;
    }

    private int UpdateStageInfo(string playEndpoint, string stageId)
    {
        int? stageIndex = null;
        foreach (var action in _playEndpoints)
        {
            if (action.Value.StageId == stageId)
            {
                stageIndex = action.Key;
                break;
            }
        }

        if(stageIndex == null)
        {
            for(int i = 0; i < 256; i++)
            {
                if (!_playEndpoints.ContainsKey(i))
                {
                    stageIndex = i;
                    break;
                }
            }
        }

        if (stageIndex == null)
        {
            stageIndex = _stageIndexGenerator.IncrementByte();
        }
        _playEndpoints[stageIndex.Value] = new TargetAddress(playEndpoint, stageId);
        return stageIndex.Value;
    }

    public void Disconnect()
    {
        if (IsAuthenticated)
        {
            IServerInfo serverInfo = FindSuitableServer(_authenticateServiceId, _authServerEndpoint);
            Packet disconnectPacket = new Packet(new DisconnectNoticeMsg());
            _sessionSender.SendToBaseApi(serverInfo.BindEndpoint(),_accountId, disconnectPacket);
            foreach (var targetId in _playEndpoints.Values)
            {
                IServerInfo targetServer = _serviceInfoCenter.FindServer(targetId.Endpoint);
                _sessionSender.SendToBaseStage(targetServer.BindEndpoint(), targetId.StageId, _accountId, disconnectPacket);
            }
        }
    }

    public void Dispatch(ClientPacket clientPacket)
    {
        try
        {
            _log.Trace(() => $"recvFrom:client - [accountId:{_accountId},packetInfo:{clientPacket.Header}]");

            ushort serviceId = clientPacket.ServiceId();
            int msgId = clientPacket.GetMsgId();


            if (IsAuthenticated)
            {
                RelayTo(serviceId, clientPacket);
            }
            else
            {
                string uri = $"{serviceId}:{msgId}";

                //for test check - don't remove
                //var packet = new ClientPacket(clientPacket.Header, new EmptyPayload());
                //RingBuffer ringBuffer = new RingBuffer(100);
                //RoutePacket.WriteClientPacketBytes(packet, ringBuffer);
                //NetMQFrame frame = new NetMQFrame(ringBuffer.Buffer(), ringBuffer.Count);
                //packet.Payload = new FramePayload(frame);
                //SendToClient(packet);
                //return;

                if (_signInUrIs.Contains(uri))
                {
                    RelayTo(serviceId, clientPacket);
                }
                else
                {
                    _log.Warn(() => $"client is not authenticated :{msgId}");
                    _session.ClientDisconnect();
                }
            }
        }catch (Exception ex)
        {
            _log.Error(()=>ex.Message);
        }
        
    }
    private IServerInfo FindSuitableServer(ushort serviceId, string endpoint)
    {
        IServerInfo serverInfo = _serviceInfoCenter.FindServer(endpoint);
        if (serverInfo.State() != ServerState.RUNNING)
        {
            serverInfo = _serviceInfoCenter.FindServerByAccountId(serviceId, _accountId);
        }
        return serverInfo;
    }
           
    private void RelayTo(ushort serviceId, ClientPacket clientPacket)
    {
        ServiceType type = _targetServiceCache.FindTypeBy(serviceId);

        IServerInfo? serverInfo = null;

        switch (type)
        {
            case ServiceType.API:
                if (string.IsNullOrEmpty(_authServerEndpoint))
                {
                    serverInfo = _serviceInfoCenter.FindRoundRobinServer(serviceId);
                }
                else
                {
                    serverInfo = FindSuitableServer(serviceId, _authServerEndpoint);
                }
                _sessionSender.RelayToApi(serverInfo.BindEndpoint(), _sid, _accountId, clientPacket);
                break;

            case ServiceType.Play:
                var targetId = _playEndpoints.GetValueOrDefault(clientPacket.Header.StageIndex);
                if (targetId == null)
                {
                    _log.Error(()=>$"Target Stage is not exist - [service type:{type}, msgId:{clientPacket.GetMsgId()}]");
                }
                else
                {
                    serverInfo = _serviceInfoCenter.FindServer(targetId.Endpoint);
                    _sessionSender.RelayToStage(serverInfo.BindEndpoint(), targetId.StageId, _sid, _accountId, clientPacket);
                }
                break;

            default:
                _log.Error(()=>$"Invalid Service Type request - [service type:{type}, msgId:{clientPacket.GetMsgId()}]");
                break;
        }
    }

    public void Send(RoutePacket routePacket)
    {
        _msgQueue.Enqueue(routePacket);
        if (_isUsing.CompareAndSet(false, true))
        {
            
                while (_msgQueue.TryDequeue(out var item))
                {
                    try
                    {
                        using (item)
                        {
                            _sessionSender.SetCurrentPacketHeader(item.RouteHeader);
                            Dispatch(item);
                        }
                    }
                    catch (Exception e)
                    {
                        _sessionSender.ErrorReply(routePacket.RouteHeader, (ushort)BaseErrorCode.SystemError);
                        _log.Error(()=>e.ToString());
                    }
                }
                _isUsing.Set(false);
        }
    }


    public void Dispatch(RoutePacket packet)
    {
        int msgId = packet.MsgId;
        bool isBase = packet.IsBase();

        if (isBase)
        {
            if(msgId == AuthenticateMsg.Descriptor.Index) 
            {
                AuthenticateMsg authenticateMsg = AuthenticateMsg.Parser.ParseFrom(packet.Data);
                var apiEndpoint = packet.RouteHeader.From;
                Authenticate((ushort)authenticateMsg.ServiceId, apiEndpoint, authenticateMsg.AccountId);
                _log.Debug(()=>$"session authenticated - [accountId:{_accountId}]");
            }
            else if(msgId == SessionCloseMsg.Descriptor.Index)
            {
                _session.ClientDisconnect();
                _log.Debug(()=>$"force session close - [accountId:{_accountId}]");
            }
            else if(msgId == JoinStageInfoUpdateReq.Descriptor.Index)
            {
                JoinStageInfoUpdateReq joinStageMsg = JoinStageInfoUpdateReq.Parser.ParseFrom(packet.Data);
                string playEndpoint = joinStageMsg.PlayEndpoint;
                string stageId = joinStageMsg.StageId;
                var stageIndex = UpdateStageInfo(playEndpoint, stageId);
                _sessionSender.Reply((ushort)BaseErrorCode.Success, 
                    XPacket.Of(new JoinStageInfoUpdateRes()
                    {
                        StageIdx = stageIndex,
                    }
                ));
                _log.Debug(()=>$"stageInfo updated - [accountId:{_accountId},playEndpoint:{playEndpoint},stageId:{stageId},stageIndex:{stageIndex}");
            }
            else if (msgId == LeaveStageMsg.Descriptor.Index)
            {
                string stageId = LeaveStageMsg.Parser.ParseFrom(packet.Data).StageId;
                ClearRoomInfo(stageId);
                _log.Debug(()=>$"stage info clear - [accountId: {_accountId}, stageId: {stageId}]");

            }
            else
            {
                _log.Error(()=>$"invalid base packet - [msgId:{msgId}]");
            }
          
        }
        else
        {
            SendToClient(packet.ToClientPacket());
        }
    }



    private void ClearRoomInfo(string stageId)
    {
        int? stageIndex = null;
        foreach (var action in _playEndpoints)
        {
            if (action.Value.StageId == stageId)
            {
                stageIndex = action.Key;
                break;
            }
        }


        if (stageIndex != null)
        {
            _playEndpoints.Remove(stageIndex.Value);
        }
    }

    private void SendToClient(ClientPacket clientPacket)
    {
        using (clientPacket)
        {
            _log.Trace(()=>$"sendTo:client - [accountId:{_accountId},packetInfo:{clientPacket.Header}]");
            _session.Send(clientPacket);
        }
    }
}
