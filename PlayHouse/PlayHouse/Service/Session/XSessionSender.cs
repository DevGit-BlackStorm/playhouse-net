using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Session
{
    public class XSessionSender : BaseSender, ISessionSender
    {
        private IClientCommunicator _clientCommunicator;

        public XSessionSender(short serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache):base(serviceId, clientCommunicator,reqCache)
        {
            this._clientCommunicator = clientCommunicator;
        }

        public void RelayToStage(string playEndpoint, long stageId, int sid, long accountId, ClientPacket packet)
        {
            var routePacket = RoutePacket.ApiOf(packet.ToPacket(), false, false);
            routePacket.RouteHeader.StageId = stageId;
            routePacket.RouteHeader.AccountId = accountId;
            routePacket.RouteHeader.Header.MsgSeq = packet.GetMsgSeq();
            routePacket.RouteHeader.Sid = sid;
            routePacket.RouteHeader.ForClient = true;
            _clientCommunicator.Send(playEndpoint, routePacket);
        }

        public void RelayToApi(string apiEndpoint, int sid, long accountId, ClientPacket packet)
        {
            var routePacket = RoutePacket.ApiOf( packet.ToPacket(), false, false);
            routePacket.RouteHeader.Sid = sid;
            routePacket.RouteHeader.Header.MsgSeq = packet.GetMsgSeq();
            routePacket.RouteHeader.ForClient = true;
            routePacket.RouteHeader.AccountId = accountId;

            _clientCommunicator.Send(apiEndpoint, routePacket);
        }
    }

   

}