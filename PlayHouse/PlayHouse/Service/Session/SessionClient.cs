using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Service.Session;
using PlayHouse.Service.Session.network;

namespace PlayHouse.Service.Session
{
    class SessionClient
    {
        //private string _serviceId;
        //private int _sid;
        //private IServerInfoCenter _serviceInfoCenter;
        //private ISession _session;
        //private IClientCommunicator _clientCommunicator;
        //private List<string> _urls;
        //private RequestCache _reqCache;

        //private XSessionSender _sessionSender;
        //private TargetServiceCache _targetServiceCache;

        //private bool _isAuthenticated = false;
        //private HashSet<string> _signInURIs = new HashSet<string>();
        //private Dictionary<string, string> _sessionData = new Dictionary<string, string>();
        //private long _accountId = 0;
        //private long _stageId = 0;
        //private string _playEndpoint = "";
        //private string _authenticateServiceId = "";

        //public SessionClient(string serviceId, int sid, IServerInfoCenter serviceInfoCenter, ISession session, IClientCommunicator clientCommunicator, List<string> urls, RequestCache reqCache)
        //{
        //    _serviceId = serviceId;
        //    _sid = sid;
        //    _serviceInfoCenter = serviceInfoCenter;
        //    _clientCommunicator = clientCommunicator;
        //    _session = session;
        //    _urls = urls;
        //    _reqCache = reqCache;

        //    _sessionSender = new XSessionSender(serviceId, clientCommunicator, reqCache);
        //    _targetServiceCache = new TargetServiceCache(serviceInfoCenter);

        //    _signInURIs.UnionWith(urls);
        //}

        //private void Authenticate(string serviceId, long accountId, string sessionInfo)
        //{
        //    _accountId = accountId;
        //    UpdateSessionInfo(serviceId, sessionInfo);
        //    _isAuthenticated = true;
        //    _authenticateServiceId = serviceId;
        //}

        //private void UpdateSessionInfo(string serviceId, string sessionInfo)
        //{
        //    _sessionData[serviceId] = sessionInfo;
        //}

        //public void Disconnect()
        //{
        //    if (_isAuthenticated)
        //    {
        //        foreach (var serverInfo in _targetServiceCache.GetTargetedServers())
        //        {
        //            Packet disconnectPacket = new Packet(new DisconnectNoticeMsg
        //            {
        //                AccountId = _accountId
        //            });

        //            if (serverInfo.ServiceType == ServiceType.API)
        //            {
        //                string sessionInfo = GetSessionInfo(serverInfo.ServiceId);
        //                _sessionSender.SendToBaseApi(serverInfo.BindEndpoint, sessionInfo, disconnectPacket);
        //            }
        //            else if (serverInfo.ServiceType == ServiceType.Play)
        //            {
        //                _sessionSender.SendToBaseStage(serverInfo.BindEndpoint, _stageId, _accountId, disconnectPacket);
        //            }
        //            else
        //            {
        //                LOG.Error($"has invalid type session data : {serverInfo.ServiceType}", this.GetType());
        //            }
        //        }
        //    }
        //}

        //public void OnReceive(ClientPacket clientPacket)
        //{
        //    string serviceId = clientPacket.ServiceId();
        //    string msgName = clientPacket.GetMsgName();

        //    if (_isAuthenticated)
        //    {
        //        RelayTo(serviceId, clientPacket);
        //    }
        //    else
        //    {
        //        string uri = $"{serviceId}:{msgName}";
        //        if (_signInURIs.Contains(uri))
        //        {
        //            RelayTo(serviceId, clientPacket);
        //        }
        //        else
        //        {
        //            LOG.Warn($"client is not authenticated :{msgName}", this.GetType());
        //        }
        //    }
        //}

        //private void RelayTo(string serviceId, ClientPacket clientPacket)
        //{
        //    string sessionInfo = GetSessionInfo(serviceId);
        //    var serverInfo = _targetServiceCache.FindServer(serviceId);
        //    string endpoint = serverInfo.BindEndpoint;
        //    ServiceType type = serverInfo.ServiceType;
        //    int msgSeq = clientPacket.Header.MsgSeq;

        //    switch (type)
        //    {
        //        case ServiceType.API:
        //            _sessionSender.RelayToApi(endpoint, _sid, sessionInfo, clientPacket.ToPacket(), msgSeq);
        //            break;
        //        case ServiceType.Play:
        //            _sessionSender.RelayToRoom(endpoint, _stageId, _sid, _accountId, sessionInfo, clientPacket.ToPacket(), msgSeq);
        //            break;
        //        default:
        //            LOG.Error($"Invalid Service Type request {type},{clientPacket.GetMsgName()}", this.GetType());
        //            break;
        //    }

        //    LOG.Debug($"session relayTo {type}:{endpoint}, sessionInfo:{sessionInfo}, msgName:{clientPacket.GetMsgName()}", this.GetType());
        //}

        //public string GetSessionInfo(string serviceId)
        //{
        //    return _sessionData.GetValueOrDefault(serviceId) ?? "";
        //}

        //public void OnReceive(RoutePacket packet)
        //{
        //    string msgName = packet.MsgName();
        //    bool isBase = packet.IsBase();

        //    if (isBase)
        //    {
        //        switch (msgName)
        //        {
        //            case "AuthenticateMsg":
        //                AuthenticateMsg authenticateMsg = AuthenticateMsg.Parser.ParseFrom(packet.Data());
        //                Authenticate(authenticateMsg.ServiceId, authenticateMsg.AccountId, authenticateMsg.SessionInfo);
        //                LOG.Debug($"{_accountId} is authenticated", this.GetType());
        //                break;
        //            case "UpdateSessionInfoMsg":
        //                UpdateSessionInfoMsg updatedSessionInfo = UpdateSessionInfoMsg.Parser.ParseFrom(packet.Data());
        //                UpdateSessionInfo(updatedSessionInfo.ServiceId, updatedSessionInfo.SessionInfo);
        //                LOG.Debug($"sessionInfo of {_accountId} is updated with {updatedSessionInfo}", this.GetType());
        //                break;
        //            case "SessionCloseMsg":
        //                _session.ClientDisconnect();
        //                LOG.Debug($"{_accountId} is required to session close", this.GetType());
        //                break;
        //            case "JoinStageMsg":
        //                JoinStageMsg joinStageMsg = JoinStageMsg.Parser.ParseFrom(packet.Data());
        //                string playEndpoint = joinStageMsg.PlayEndpoint;
        //                long stageId = joinStageMsg.StageId;
        //                UpdateRoomInfo(playEndpoint, stageId);
        //                LOG.Debug($"{_accountId} is roomInfo updated:{playEndpoint},{stageId} $", this.GetType());
        //                break;
        //            case "LeaveStageMsg":
        //                ClearRoomInfo();
        //                LOG.Debug($"{_accountId} is roomInfo clear:{_playEndpoint},{_stageId} $", this.GetType());
        //                break;
        //            default:
        //                LOG.Error($"Invalid Packet {msgName}", this.GetType());
        //                break;
        //        }
        //    }
        //    else
        //    {
        //        SendToClient(packet.ToClientPacket());
        //    }
        //}

        //private void UpdateRoomInfo(string playEndpoint, long stageId)
        //{
        //    _playEndpoint = playEndpoint;
        //    _stageId = stageId;
        //}

        //private void ClearRoomInfo()
        //{
        //    _playEndpoint = "";
        //    _stageId = 0;
        //}

        //private void SendToClient(ClientPacket clientPacket)
        //{
        //    _session.Send(clientPacket);
        //}
    }
}