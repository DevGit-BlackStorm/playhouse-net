using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Service.Play.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play
{
    public class XActorSender : IActorSender
    {
        private readonly long _accountId;
        private string _sessionEndpoint;
        private int _sid;
        private string _apiEndpoint;
        private readonly BaseStage _baseStage;
        private readonly IServerInfoCenter _serverInfoCenter;

        public XActorSender(long accountId, string sessionEndpoint, int sid, string apiEndpoint, BaseStage baseStage, IServerInfoCenter serverInfoCenter)
        {
            _accountId = accountId;
            _sessionEndpoint = sessionEndpoint;
            _sid = sid;
            _apiEndpoint = apiEndpoint;
            _baseStage = baseStage;
            _serverInfoCenter = serverInfoCenter;
        }

        public string SessionEndpoint() => _sessionEndpoint;

        public string ApiEndpoint() => _apiEndpoint;

        public int Sid() => _sid;

        public long AccountId() => _accountId;

        public void LeaveStage()
        {
            _baseStage.LeaveStage(_accountId, _sessionEndpoint, _sid);
        }

        public void SendToClient(Packet packet)
        {
            _baseStage.StageSender.SendToClient(_sessionEndpoint, _sid, packet);
        }

        public void SendToApi(Packet packet)
        {
            var serverInfo = _serverInfoCenter.FindServer(_apiEndpoint);
            if (!serverInfo.IsValid())
            {
                serverInfo = _serverInfoCenter.FindServerByAccountId(serverInfo.ServiceId, _accountId);
            }
            _baseStage.StageSender.SendToApi(serverInfo.BindEndpoint, _accountId, packet);
        }

        public async Task<ReplyPacket> RequestToApi(Packet packet)
        {
            var serverInfo = _serverInfoCenter.FindServer(_apiEndpoint);
            if (!serverInfo.IsValid())
            {
                serverInfo = _serverInfoCenter.FindServerByAccountId(serverInfo.ServiceId, _accountId);
            }
            return await _baseStage.StageSender.RequestToApi(serverInfo.BindEndpoint, _accountId, packet);
        }

        public async Task<ReplyPacket> AsyncToApi(Packet packet)
        {
            var serverInfo = _serverInfoCenter.FindServer(_apiEndpoint);
            if (!serverInfo.IsValid())
            {
                serverInfo = _serverInfoCenter.FindServerByAccountId(serverInfo.ServiceId, _accountId);
            }
            return await _baseStage.StageSender.AsyncToApi(serverInfo.BindEndpoint, _accountId, packet);
        }

        public void Update(string sessionEndpoint, int sid, string apiEndpoint)
        {
            _sessionEndpoint = sessionEndpoint;
            _sid = sid;
            _apiEndpoint = apiEndpoint;
        }
    }
}
