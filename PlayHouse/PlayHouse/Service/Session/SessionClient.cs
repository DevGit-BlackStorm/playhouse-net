using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Service.Session.network;
using Google.Protobuf;
using System;
using System.Runtime.InteropServices;
using System.Linq;

namespace PlayHouse.Service.Session
{
    public class TargetAddress
    {
        public string Endpoint { get; }
        public long StageId { get; }

        public TargetAddress(string endpoint, long stageId = 0)
        {
            Endpoint = endpoint;
            StageId = stageId;
        }
    }
    class StageIndexGenerator
    {
        private byte _byteValue = 0;

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
        private short _serviceId;
        private int _sid;
        private IServerInfoCenter _serviceInfoCenter;
        private ISession _session;
        private IClientCommunicator _clientCommunicator;
        private List<string> _urls;
        private RequestCache _reqCache;

        private XSessionSender _sessionSender;
        private TargetServiceCache _targetServiceCache;

        public  bool IsAuthenticated { get; private set; } = false;
        private HashSet<string> _signInURIs = new HashSet<string>();
        //private Dictionary<short, string> _sessionData = new Dictionary<short, string>();
        private long _accountId = 0;

        private Dictionary<int, TargetAddress> _playEndpoints = new Dictionary<int, TargetAddress>();
        private short _authenticateServiceId = 0;
        private string _authServerEndpoint = "";
        private StageIndexGenerator _stageIndexGenerator = new StageIndexGenerator();

        public SessionClient(
            short serviceId, 
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

            _signInURIs.UnionWith(urls);
        }


        private void Authenticate(short serviceId, string apiEndpoint, long accountId)
        {
            this._accountId = accountId;
            this.IsAuthenticated = true;
            this._authenticateServiceId = serviceId;
            this._authServerEndpoint = apiEndpoint;
        }

        private int UpdateStageInfo(string playEndpoint, long stageId)
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
                for(int i = 1; i < 256; i++)
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
                Packet disconnectPacket = new Packet(new DisconnectNoticeMsg
                {
                    AccountId = _accountId
                });
                _sessionSender.SendToBaseApi(serverInfo.BindEndpoint(), disconnectPacket);
                foreach (var targetId in _playEndpoints.Values)
                {
                    IServerInfo targetServer = _serviceInfoCenter.FindServer(targetId.Endpoint);
                    _sessionSender.SendToBaseStage(targetServer.BindEndpoint(), targetId.StageId, _accountId, disconnectPacket);
                }
            }
        }

        public void OnReceive(ClientPacket clientPacket)
        {
            short serviceId = clientPacket.ServiceId();
            int msgId = clientPacket.GetMsgId();

            if (IsAuthenticated)
            {
                RelayTo(serviceId, clientPacket);
            }
            else
            {
                string uri = $"{serviceId}:{msgId}";
                if (_signInURIs.Contains(uri))
                {
                    RelayTo(serviceId, clientPacket);
                }
                else
                {
                    LOG.Warn($"client is not authenticated :{msgId}", this.GetType());
                    _session.ClientDisconnect();
                }
            }
        }
        private IServerInfo FindSuitableServer(short serviceId, string endpoint)
        {
            IServerInfo serverInfo = _serviceInfoCenter.FindServer(endpoint);
            if (serverInfo.State() != ServerState.RUNNING)
            {
                serverInfo = _serviceInfoCenter.FindServerByAccountId(serviceId, _accountId);
            }
            return serverInfo;
        }
               
        private void RelayTo(short serviceId, ClientPacket clientPacket)
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
                    var targetId = _playEndpoints[clientPacket.Header.StageIndex];
                    if (targetId == null)
                    {
                        LOG.Error($"Target Stage is not exist - service type:{type}, msgId:{clientPacket.GetMsgId()}", this.GetType());
                    }
                    else
                    {
                        serverInfo = _serviceInfoCenter.FindServer(targetId.Endpoint);
                        _sessionSender.RelayToStage(serverInfo.BindEndpoint(), targetId.StageId, _sid, _accountId, clientPacket);
                    }
                    break;

                default:
                    LOG.Error($"Invalid Service Type request - service type:{type}, msgId:{clientPacket.GetMsgId()}", this.GetType());
                    break;
            }
        }

   

        public void OnReceive(RoutePacket packet)
        {
            int msgId = packet.GetMsgId();
            bool isBase = packet.IsBase();

            if (isBase)
            {
                if(msgId == AuthenticateMsg.Descriptor.Index) 
                {
                    AuthenticateMsg authenticateMsg = AuthenticateMsg.Parser.ParseFrom(packet.Data);
                    var apiEndpoint = packet.RouteHeader.From;
                    Authenticate((short)authenticateMsg.ServiceId, apiEndpoint,authenticateMsg.AccountId);
                    LOG.Debug($"{_accountId} is authenticated", this.GetType());
                }
                else if(msgId != SessionCloseMsg.Descriptor.Index)
                {
                    _session.ClientDisconnect();
                    LOG.Debug($"{_accountId} is required to session close", this.GetType());
                }
                else if(msgId != JoinStageInfoUpdateReq.Descriptor.Index)
                {
                    JoinStageInfoUpdateReq joinStageMsg = JoinStageInfoUpdateReq.Parser.ParseFrom(packet.Data);
                    string playEndpoint = joinStageMsg.PlayEndpoint;
                    long stageId = joinStageMsg.StageId;
                    var stageIndex = UpdateStageInfo(playEndpoint, stageId);
                    LOG.Debug($"{_accountId} is stageInfo updated:{playEndpoint},{stageId} $", this.GetType());
                }
                else if (msgId != LeaveStageMsg.Descriptor.Index)
                {
                    var stageId = LeaveStageMsg.Parser.ParseFrom(packet.Data).StageId;
                    ClearRoomInfo(stageId);
                    LOG.Debug($"stage info clear - accountId: {_accountId}, stageId: {stageId}", this.GetType());

                }
                else
                {
                    LOG.Error($"Invalid Packet {msgId}", this.GetType());
                }
              
            }
            else
            {
                SendToClient(packet.ToClientPacket());
            }
        }



        private void ClearRoomInfo(long stageId)
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
                _session.Send(clientPacket);
            }
        }
    }
}