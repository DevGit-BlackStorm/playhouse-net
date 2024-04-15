using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Service.Session.Network;
using PlayHouse.Utils;
using System.Collections.Concurrent;
using CommonLib;
using PlayHouse.Service.Shared;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Session;
internal class TargetAddress
{
    public string Endpoint { get; }
    public long StageId { get; }

    public TargetAddress(string endpoint, long stageId)
    {
        Endpoint = endpoint;
        StageId = stageId;
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

internal class SessionActor
{
    private readonly LOG<SessionActor> _log = new ();
    private readonly int _sid;
    private readonly IServerInfoCenter _serviceInfoCenter;
    private readonly ISession _session;

    private readonly XSessionSender _sessionSender;
    private readonly TargetServiceCache _targetServiceCache;
    private readonly ConcurrentQueue<RoutePacket> _msgQueue = new();
    private readonly AtomicBoolean _isUsing = new(false);

    public  bool IsAuthenticated { get; private set; }
    private readonly HashSet<string> _signInUrIs = new();
    private long _accountId;

    private readonly Dictionary<long, TargetAddress> _playEndpoints = new();
    private ushort _authenticateServiceId;
    private string _authServerEndpoint = "";
    private readonly StageIndexGenerator _stageIndexGenerator = new();
    private DateTime _lastUpdateTime = DateTime.UtcNow;
    private PooledByteBuffer _heartbeatBuffer = new PooledByteBuffer(100);
    private bool _debugMode = false;

    public SessionActor(
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


    private void Authenticate(ushort serviceId, string apiEndpoint, long accountId)
    {
        _accountId = accountId;
        IsAuthenticated = true;
        _authenticateServiceId = serviceId;
        _authServerEndpoint = apiEndpoint;
    }

    private void  UpdateStageInfo(string playEndpoint, long stageId)
    {
        //int? stageIndex = null;


        //foreach (var action in _playEndpoints)
        //{
        //    if (action.Value.StageId == stageId)
        //    {
        //        stageIndex = action.Key;
        //        break;
        //    }
        //}

        //if(stageIndex == null)
        //{
        //    for(int i = 0; i < 256; i++)
        //    {
        //        if (!_playEndpoints.ContainsKey(i))
        //        {
        //            stageIndex = i;
        //            break;
        //        }
        //    }
        //}

        //if (stageIndex == null)
        //{
        //    stageIndex = _stageIndexGenerator.IncrementByte();
        //}
        _playEndpoints[stageId] = new TargetAddress(playEndpoint, stageId);
        //return stageIndex.Value;
    }

    public void Disconnect()
    {
        if (IsAuthenticated)
        {
            IServerInfo serverInfo = FindSuitableServer(_authenticateServiceId, _authServerEndpoint);
            RoutePacket disconnectPacket = RoutePacket.Of(new DisconnectNoticeMsg());
            _sessionSender.SendToBaseApi(serverInfo.GetBindEndpoint(),_accountId, disconnectPacket);
            foreach (var targetId in _playEndpoints.Values)
            {
                IServerInfo targetServer = _serviceInfoCenter.FindServer(targetId.Endpoint);
                _sessionSender.SendToBaseStage(targetServer.GetBindEndpoint(), targetId.StageId, _accountId, disconnectPacket);
            }
        }
    }

    public void ClientDisconnect()
    {
        _session.ClientDisconnect();
    }

    public void Dispatch(ClientPacket clientPacket)
    {
        try
        {
            _log.Trace(() => $"recvFrom:client - [accountId:{_accountId},packetInfo:{clientPacket.Header}]");

            ushort serviceId = clientPacket.ServiceId;
            string msgId = clientPacket.MsgId;

            UpdateHeartBeatTime();

            if (msgId == "-1") //heartbeat
            {
                SendHeartBeat(clientPacket);
                return;
            }

            if(msgId == "-2") //debug mode
            {
                _log.Debug(() => $"session is debug mode - [sid:{_sid}]");
                _debugMode = true;
                return;
            }

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

    private void SendHeartBeat(ClientPacket clientPacket)
    {
        
        //_log.Trace(() => $"send heartbeat - [packet:{clientPacket.Header}]");
        RoutePacket.WriteClientPacketBytes(clientPacket, _heartbeatBuffer);
        var reply = new ClientPacket(clientPacket.Header, new PooledBytePayload(_heartbeatBuffer));
        SendToClient(reply);
        _heartbeatBuffer.Clear();
    }

    private IServerInfo FindSuitableServer(ushort serviceId, string endpoint)
    {
        IServerInfo serverInfo = _serviceInfoCenter.FindServer(endpoint);
        if (serverInfo.GetState() != ServerState.RUNNING)
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
                _sessionSender.RelayToApi(serverInfo.GetBindEndpoint(), _sid, _accountId, clientPacket);
                break;

            case ServiceType.Play:
                var targetId = _playEndpoints.GetValueOrDefault(clientPacket.Header.StageId);
                if (targetId == null)
                {
                    _log.Error(()=>$"Target Stage is not exist - [service type:{type}, msgId:{clientPacket.MsgId}]");
                }
                else
                {
                    serverInfo = _serviceInfoCenter.FindServer(targetId.Endpoint);
                    _sessionSender.RelayToStage(serverInfo.GetBindEndpoint(), targetId.StageId, _sid, _accountId, clientPacket);
                }
                break;

            default:
                _log.Error(()=>$"Invalid Service Type request - [service type:{type}, msgId:{clientPacket.MsgId}]");
                break;
        }
    }

    public async Task PostAsync(RoutePacket routePacket)
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
                            await DispatchAsync(item);
                        }
                    }
                    catch (Exception e)
                    {
                        _sessionSender.Reply((ushort)BaseErrorCode.SystemError);
                        _log.Error(()=>e.ToString());
                    }
                }
                _isUsing.Set(false);
        }
    }


    public async Task DispatchAsync(RoutePacket packet)
    {
        string msgId = packet.MsgId;
        bool isBase = packet.IsBase();

        if (isBase)
        {
            if(msgId == AuthenticateMsg.Descriptor.Name) 
            {
                AuthenticateMsg authenticateMsg = AuthenticateMsg.Parser.ParseFrom(packet.Span);
                var apiEndpoint = packet.RouteHeader.From;
                Authenticate((ushort)authenticateMsg.ServiceId, apiEndpoint, authenticateMsg.AccountId);
                _log.Debug(()=>$"session authenticated - [accountId:{_accountId}]");
            }
            else if(msgId == SessionCloseMsg.Descriptor.Name)
            {
                _session.ClientDisconnect();
                _log.Debug(()=>$"force session close - [accountId:{_accountId}]");
            }
            else if(msgId == JoinStageInfoUpdateReq.Descriptor.Name)
            {
                JoinStageInfoUpdateReq joinStageMsg = JoinStageInfoUpdateReq.Parser.ParseFrom(packet.Span);
                string playEndpoint = joinStageMsg.PlayEndpoint;
                long stageId = joinStageMsg.StageId;
                UpdateStageInfo(playEndpoint, stageId);

                _sessionSender.Reply(XPacket.Of(new JoinStageInfoUpdateRes()));

                _log.Debug(()=>$"stageInfo updated - [accountId:{_accountId},playEndpoint:{playEndpoint},stageId:{stageId}");
            }
            else if (msgId == LeaveStageMsg.Descriptor.Name)
            {
                long stageId = LeaveStageMsg.Parser.ParseFrom(packet.Span).StageId;
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

        await Task.CompletedTask;
    }



    private void ClearRoomInfo(long stageId)
    {
        if(_playEndpoints.ContainsKey(stageId)) 
        {
            _playEndpoints.Remove(stageId);
        }
        
        //int? stageIndex = null;
        //foreach (var action in _playEndpoints)
        //{
        //    if (action.Value.StageId == stageId)
        //    {
        //        stageIndex = action.Key;
        //        break;
        //    }
        //}


        //if (stageIndex != null)
        //{
        //    _playEndpoints.Remove(stageIndex.Value);
        //}
    }

    private void SendToClient(ClientPacket clientPacket)
    {
        using (clientPacket)
        {
            _log.Trace(()=>$"sendTo:client - [accountId:{_accountId},packetInfo:{clientPacket.Header}]");
            _session.Send(clientPacket);
        }
    }

    internal bool IsIdleState(int idleTime)
    {
        if(IsAuthenticated == false)
        { 
            return false;
        }

        if (_debugMode)
        {
            return false;
        }

        if(idleTime <= 0)
        {
            return false;
        }

        var timeDifference =  DateTime.UtcNow - _lastUpdateTime;
        if (timeDifference.TotalMilliseconds > idleTime )
        {
            return true;
        }
        return false;
    }

    internal void UpdateHeartBeatTime()
    {
        _lastUpdateTime = DateTime.UtcNow;
    }

    internal long IdleTime()
    {
        var timeDifference = DateTime.UtcNow - _lastUpdateTime;
        return ((long)timeDifference.TotalMilliseconds);
    }

    internal long AccountId => _accountId;
    internal int Sid => _sid;
}
