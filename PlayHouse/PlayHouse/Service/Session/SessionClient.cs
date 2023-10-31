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
public class TargetAddress
{
    public string Endpoint { get; }
    public Guid StageId { get; }

    public TargetAddress(string endpoint, Guid? stageId = null)
    {
        Endpoint = endpoint;
        StageId = stageId ?? Guid.Empty;
    }
}
class StageIndexGenerator
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

public class SessionClient
{
    private ushort _serviceId;
    private readonly int _sid;
    private readonly IServerInfoCenter _serviceInfoCenter;
    private readonly ISession _session;
    private IClientCommunicator _clientCommunicator;
    private List<string> _urls;
    private RequestCache _reqCache;

    private readonly XSessionSender _sessionSender;
    private readonly TargetServiceCache _targetServiceCache;
    private readonly ConcurrentQueue<RoutePacket> _msgQueue = new();
    private readonly AtomicBoolean _isUsing = new(false);

    public  bool IsAuthenticated { get; private set; }
    private readonly HashSet<string> _signInUrIs = new();
    private Guid _accountId = Guid.Empty;

    private readonly Dictionary<int, TargetAddress> _playEndpoints = new();
    private ushort _authenticateServiceId;
    private string _authServerEndpoint = "";
    private readonly StageIndexGenerator _stageIndexGenerator = new();

    public SessionClient(
        ushort serviceId, 
        int sid, 
        IServerInfoCenter serviceInfoCenter, 
        ISession session, 
        IClientCommunicator clientCommunicator, 
        List<string> urls, 
        RequestCache reqCache
        )
    {
        _serviceId = serviceId;
        _sid = sid;
        _serviceInfoCenter = serviceInfoCenter;
        _clientCommunicator = clientCommunicator;
        _session = session;
        _urls = urls;
        _reqCache = reqCache;

        _sessionSender = new XSessionSender(serviceId, clientCommunicator, reqCache);
        _targetServiceCache = new TargetServiceCache(serviceInfoCenter);

        _signInUrIs.UnionWith(urls);
    }


    private void Authenticate(ushort serviceId, string apiEndpoint, Guid accountId)
    {
        _accountId = accountId;
        IsAuthenticated = true;
        _authenticateServiceId = serviceId;
        _authServerEndpoint = apiEndpoint;
    }

    private int UpdateStageInfo(string playEndpoint, Guid stageId)
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
            ushort serviceId = clientPacket.ServiceId();
            int msgId = clientPacket.GetMsgId();

            //RelayTo(serviceId, clientPacket);

            if (IsAuthenticated)
            {
                RelayTo(serviceId, clientPacket);
            }
            else
            {
                string uri = $"{serviceId}:{msgId}";

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
                    LOG.Warn(() => $"client is not authenticated :{msgId}", this.GetType());
                    _session.ClientDisconnect();
                }
            }
        }catch (Exception ex)
        {
            LOG.Error(()=>ex.Message, this.GetType());
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
                    LOG.Error(()=>$"Target Stage is not exist - service type:{type}, msgId:{clientPacket.GetMsgId()}", this.GetType());
                }
                else
                {
                    serverInfo = _serviceInfoCenter.FindServer(targetId.Endpoint);
                    _sessionSender.RelayToStage(serverInfo.BindEndpoint(), targetId.StageId, _sid, _accountId, clientPacket);
                }
                break;

            default:
                LOG.Error(()=>$"Invalid Service Type request - service type:{type}, msgId:{clientPacket.GetMsgId()}", this.GetType());
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
                        LOG.Error(()=>e.Message, this.GetType());
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
                Authenticate((ushort)authenticateMsg.ServiceId, apiEndpoint, new Guid(authenticateMsg.AccountId.ToByteArray()));
                LOG.Debug(()=>$"{_accountId} is authenticated", this.GetType());
            }
            else if(msgId == SessionCloseMsg.Descriptor.Index)
            {
                _session.ClientDisconnect();
                LOG.Debug(()=>$"{_accountId} is required to session close", this.GetType());
            }
            else if(msgId == JoinStageInfoUpdateReq.Descriptor.Index)
            {
                JoinStageInfoUpdateReq joinStageMsg = JoinStageInfoUpdateReq.Parser.ParseFrom(packet.Data);
                string playEndpoint = joinStageMsg.PlayEndpoint;
                Guid stageId = new Guid(joinStageMsg.StageId.ToByteArray());
                var stageIndex = UpdateStageInfo(playEndpoint, stageId);
                _sessionSender.Reply(
                    new ReplyPacket(new JoinStageInfoUpdateRes()
                    {
                        StageIdx = stageIndex,
                    })
                );
                LOG.Debug(()=>$"{_accountId} is stageInfo updated: playEndpoint:{playEndpoint},stageId:{stageId}, stageIndex:{stageIndex}", this.GetType());
            }
            else if (msgId == LeaveStageMsg.Descriptor.Index)
            {
                Guid stageId = new (LeaveStageMsg.Parser.ParseFrom(packet.Data).StageId.ToByteArray());
                ClearRoomInfo(stageId);
                LOG.Debug(()=>$"stage info clear - accountId: {_accountId}, stageId: {stageId}", this.GetType());

            }
            else
            {
                LOG.Error(()=>$"Invalid Packet {msgId}", this.GetType());
            }
          
        }
        else
        {
            SendToClient(packet.ToClientPacket());
        }
    }



    private void ClearRoomInfo(Guid stageId)
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
            LOG.Trace(()=>$"SendTo Client - PacketInfo:{clientPacket.Header}",this.GetType());
            _session.Send(clientPacket);
        }
    }
}
