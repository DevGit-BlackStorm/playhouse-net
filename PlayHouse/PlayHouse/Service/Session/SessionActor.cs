using System.Collections.Concurrent;
using System.Diagnostics;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;
using PlayHouse.Service.Session.Network;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session;

internal class TargetAddress(string endpoint, long stageId)
{
    public string Endpoint { get; } = endpoint;
    public long StageId { get; } = stageId;
}

internal class StageIndexGenerator
{
    private byte _byteValue;

    public byte IncrementByte()
    {
        _byteValue = (byte)((_byteValue + 1) & 0xff);
        if (_byteValue == 0)
        {
            _byteValue = IncrementByte();
        }

        return _byteValue;
    }
}

internal class SessionActor
{
    private readonly PooledByteBuffer _heartbeatBuffer = new(100);
    private readonly AtomicBoolean _isUsing = new(false);
    private readonly LOG<SessionActor> _log = new();
    private readonly ConcurrentQueue<RoutePacket> _msgQueue = new();

    private readonly Dictionary<long, TargetAddress> _playEndpoints = new();
    private readonly IServerInfoCenter _serviceInfoCenter;
    private readonly ISession _session;

    private readonly XSessionSender _sessionSender;
    private readonly HashSet<string> _signInUrIs = new();
    private readonly StageIndexGenerator _stageIndexGenerator = new();
    private readonly TargetServiceCache _targetServiceCache;
    private ushort _authenticateServiceId;
    private string _authServerEndpoint = "";
    private bool _debugMode;
    private Stopwatch _lastUpdateTime = new();
    private string _remoteIp = string.Empty;

    public SessionActor(
        ushort serviceId,
        long sid,
        IServerInfoCenter serviceInfoCenter,
        ISession session,
        IClientCommunicator clientCommunicator,
        List<string> urls,
        RequestCache reqCache,
        string remoteIp
    )
    {
        Sid = sid;
        _serviceInfoCenter = serviceInfoCenter;
        _session = session;

        _sessionSender = new XSessionSender(serviceId, clientCommunicator, reqCache);
        _targetServiceCache = new TargetServiceCache(serviceInfoCenter);

        _signInUrIs.UnionWith(urls);
        _remoteIp = remoteIp;
    }

    public bool IsAuthenticated { get; private set; }

    internal long AccountId { get; private set; }

    internal long Sid { get; }


    private void Authenticate(ushort serviceId, string apiEndpoint, long accountId)
    {
        AccountId = accountId;
        IsAuthenticated = true;
        _authenticateServiceId = serviceId;
        _authServerEndpoint = apiEndpoint;

        _lastUpdateTime.Start();
    }

    private void UpdateStageInfo(string playEndpoint, long stageId)
    {
        _playEndpoints[stageId] = new TargetAddress(playEndpoint, stageId);
    }

    public void Disconnect()
    {
        if (IsAuthenticated)
        {
            var serverInfo = FindSuitableServer(_authenticateServiceId, _authServerEndpoint);
            var disconnectPacket = RoutePacket.Of(new DisconnectNoticeMsg());
            _sessionSender.SendToBaseApi(serverInfo.GetBindEndpoint(), AccountId, disconnectPacket);
            foreach (var targetId in _playEndpoints.Values)
            {
                IServerInfo targetServer = _serviceInfoCenter.FindServer(targetId.Endpoint);
                _sessionSender.SendToBaseStage(targetServer.GetBindEndpoint(), targetId.StageId, AccountId,
                    disconnectPacket);
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
            _log.Trace(() => $"recvFrom:client - [accountId:{AccountId},packetInfo:{clientPacket.Header}]");

            var serviceId = clientPacket.ServiceId;
            var msgId = clientPacket.MsgId;

            _lastUpdateTime.Restart();

            if (msgId == PacketConst.HeartBeat) //heartbeat
            {
                SendHeartBeat(clientPacket);
                return;
            }

            if (msgId == PacketConst.Debug) //debug mode
            {
                _log.Debug(() => $"session is debug mode - [sid:{Sid}]");
                _debugMode = true;
                return;
            }

            if (IsAuthenticated)
            {
                RelayTo(serviceId, clientPacket);
            }
            else
            {
                var uri = $"{serviceId}:{msgId}";

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
        }
        catch (Exception ex)
        {
            _log.Error(() => $"{ex.Message}");
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
            serverInfo = _serviceInfoCenter.FindServerByAccountId(serviceId, AccountId);
        }

        return serverInfo;
    }

    private void RelayTo(ushort serviceId, ClientPacket clientPacket)
    {
        var type = _targetServiceCache.FindTypeBy(serviceId);

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

                _sessionSender.RelayToApi(serverInfo.GetBindEndpoint(), Sid, AccountId, clientPacket);
                break;

            case ServiceType.Play:
                var targetId = _playEndpoints.GetValueOrDefault(clientPacket.Header.StageId);
                if (targetId == null)
                {
                    _log.Error(() => $"Target Stage is not exist - [service type:{type}, msgId:{clientPacket.MsgId}]");
                }
                else
                {
                    serverInfo = _serviceInfoCenter.FindServer(targetId.Endpoint);
                    _sessionSender.RelayToStage(serverInfo.GetBindEndpoint(), targetId.StageId, Sid, AccountId,
                        clientPacket);
                }

                break;

            default:
                _log.Error(() => $"Invalid Service Type request - [service type:{type}, msgId:{clientPacket.MsgId}]");
                break;
        }
    }

    public void Post(RoutePacket routePacket)
    {
        _msgQueue.Enqueue(routePacket);
        if (_isUsing.CompareAndSet(false, true))
        {
            Task.Run(async () =>
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
                        _log.Error(() => $"{e}");
                    }
                }

                _isUsing.Set(false);
            });
        }
    }


    public async Task DispatchAsync(RoutePacket packet)
    {
        var msgId = packet.MsgId;
        var isBase = packet.IsBase();

        if (isBase)
        {
            if (msgId == AuthenticateMsg.Descriptor.Name)
            {
                var authenticateMsg = AuthenticateMsg.Parser.ParseFrom(packet.Span);
                var apiEndpoint = packet.RouteHeader.From;
                Authenticate((ushort)authenticateMsg.ServiceId, apiEndpoint, authenticateMsg.AccountId);
                _log.Debug(() => $"session authenticated - [accountId:{AccountId}]");
            }
            else if (msgId == SessionCloseMsg.Descriptor.Name)
            {
                _session.ClientDisconnect();
                _log.Debug(() => $"force session close - [accountId:{AccountId}]");
            }
            else if (msgId == JoinStageInfoUpdateReq.Descriptor.Name)
            {
                var joinStageMsg = JoinStageInfoUpdateReq.Parser.ParseFrom(packet.Span);
                var playEndpoint = joinStageMsg.PlayEndpoint;
                var stageId = joinStageMsg.StageId;
                UpdateStageInfo(playEndpoint, stageId);

                _sessionSender.Reply(XPacket.Of(new JoinStageInfoUpdateRes()));

                _log.Debug(() =>
                    $"stageInfo updated - [accountId:{AccountId},playEndpoint:{playEndpoint},stageId:{stageId}");
            }
            else if (msgId == LeaveStageMsg.Descriptor.Name)
            {
                var stageId = LeaveStageMsg.Parser.ParseFrom(packet.Span).StageId;
                ClearRoomInfo(stageId);
                _log.Debug(() => $"stage info clear - [accountId: {AccountId}, stageId: {stageId}]");
            }
            else if (msgId == RemoteIpReq.Descriptor.Name)
            {
                _sessionSender.Reply(XPacket.Of(new RemoteIpRes(){Ip = _remoteIp }));
            }
            else
            {
                _log.Error(() => $"invalid base packet - [msgId:{msgId}]");
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
        if (_playEndpoints.ContainsKey(stageId))
        {
            _playEndpoints.Remove(stageId);
        }
    }

    private void SendToClient(ClientPacket clientPacket)
    {
        using (clientPacket)
        {
            _log.Trace(() => $"sendTo:client - [accountId:{AccountId},packetInfo:{clientPacket.Header}]");
            _session.Send(clientPacket);
        }
    }

    internal bool IsIdleState(int idleTime)
    {
        if (IsAuthenticated == false)
        {
            return false;
        }

        if (_debugMode)
        {
            return false;
        }

        if (idleTime <= 0)
        {
            return false;
        }

        if (_lastUpdateTime.ElapsedMilliseconds > idleTime)
        {
            return true;
        }

        return false;
    }

    //internal void UpdateHeartBeatTime()
    //{
    //    _lastUpdateTime = DateTime.UtcNow;
    //}

    internal long IdleTime()
    {
        return _lastUpdateTime.ElapsedMilliseconds;
    }
}