using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Service.Session.network;
using Google.Protobuf;
using System;

namespace PlayHouse.Service.Session
{

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
        private Dictionary<short, string> _sessionData = new Dictionary<short, string>();
        private long _accountId = 0;
        private long _stageId = 0;
        private string _playEndpoint = "";
        private short _authenticateServiceId;

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


        private void Authenticate(short serviceId, long accountId, string sessionInfo)
        {
            _accountId = accountId;
            UpdateSessionInfo(serviceId, sessionInfo);
            IsAuthenticated = true;
            _authenticateServiceId = serviceId;
        }

        private void UpdateSessionInfo(short serviceId, string sessionInfo)
        {
            _sessionData[serviceId] = sessionInfo;
        }

        public void Disconnect()
        {
            if (IsAuthenticated)
            {
                foreach (var serverInfo in _targetServiceCache.GetTargetedServers())
                {
                    Packet disconnectPacket = new Packet(new DisconnectNoticeMsg
                    {
                        AccountId = _accountId
                    });

                    if (serverInfo.ServiceType == ServiceType.API)
                    {
                        string sessionInfo = GetSessionInfo(serverInfo.ServiceId);
                        _sessionSender.SendToBaseApi(serverInfo.BindEndpoint, sessionInfo, disconnectPacket);
                    }
                    else if (serverInfo.ServiceType == ServiceType.Play)
                    {
                        _sessionSender.SendToBaseStage(serverInfo.BindEndpoint, _stageId, _accountId, disconnectPacket);
                    }
                    else
                    {
                        LOG.Error($"has invalid type session data : {serverInfo.ServiceType}", this.GetType());
                    }
                }
            }
        }

        public void OnReceive(ClientPacket clientPacket)
        {
            short serviceId = clientPacket.ServiceId();
            short msgId = clientPacket.GetMsgId();

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

        private void RelayTo(short serviceId, ClientPacket clientPacket)
        {
            string sessionInfo = GetSessionInfo(serviceId);
            var serverInfo = _targetServiceCache.FindServer(serviceId);
            string endpoint = serverInfo.BindEndpoint;
            ServiceType type = serverInfo.ServiceType;
            short msgSeq = clientPacket.Header.MsgSeq;

            switch (type)
            {
                case ServiceType.API:
                    _sessionSender.RelayToApi(endpoint, _sid, sessionInfo, clientPacket.ToPacket(), msgSeq);
                    break;
                case ServiceType.Play:
                    _sessionSender.RelayToRoom(endpoint, _stageId, _sid, _accountId, sessionInfo, clientPacket.ToPacket(), msgSeq);
                    break;
                default:
                    LOG.Error($"Invalid Service Type request {type},{clientPacket.GetMsgId()}", this.GetType());
                    break;
            }

            LOG.Debug($"session relayTo {type}:{endpoint}, sessionInfo:{sessionInfo}, msgName:{clientPacket.GetMsgId()}", this.GetType());
        }

        public string GetSessionInfo(short serviceId)
        {
            return _sessionData.GetValueOrDefault(serviceId) ?? "";
        }

        public void OnReceive(RoutePacket packet)
        {
            int msgId = packet.GetMsgId();
            bool isBase = packet.IsBase();

            if (isBase)
            {
                if(msgId == AuthenticateMsg.Descriptor.Index) 
                {
                    AuthenticateMsg authenticateMsg = AuthenticateMsg.Parser.ParseFrom(packet.Data());
                    Authenticate((short)authenticateMsg.ServiceId, authenticateMsg.AccountId, authenticateMsg.SessionInfo);
                    LOG.Debug($"{_accountId} is authenticated", this.GetType());
                }
                else if(msgId != UpdateSessionInfoMsg.Descriptor.Index)
                {
                    UpdateSessionInfoMsg updatedSessionInfo = UpdateSessionInfoMsg.Parser.ParseFrom(packet.Data());
                    UpdateSessionInfo((short)updatedSessionInfo.ServiceId, updatedSessionInfo.SessionInfo);
                    LOG.Debug($"sessionInfo of {_accountId} is updated with {updatedSessionInfo}", this.GetType());

                }
                else if(msgId != SessionCloseMsg.Descriptor.Index)
                {
                    _session.ClientDisconnect();
                    LOG.Debug($"{_accountId} is required to session close", this.GetType());
                }
                else if(msgId != JoinStageMsg.Descriptor.Index)
                {
                    JoinStageMsg joinStageMsg = JoinStageMsg.Parser.ParseFrom(packet.Data());
                    string playEndpoint = joinStageMsg.PlayEndpoint;
                    long stageId = joinStageMsg.StageId;
                    UpdateRoomInfo(playEndpoint, stageId);
                    LOG.Debug($"{_accountId} is roomInfo updated:{playEndpoint},{stageId} $", this.GetType());
                }
                else if (msgId != LeaveStageMsg.Descriptor.Index)
                {
                    ClearRoomInfo();
                    LOG.Debug($"{_accountId} is roomInfo clear:{_playEndpoint},{_stageId} $", this.GetType());
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

        private void UpdateRoomInfo(string playEndpoint, long stageId)
        {
            _playEndpoint = playEndpoint;
            _stageId = stageId;
        }

        private void ClearRoomInfo()
        {
            _playEndpoint = "";
            _stageId = 0;
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