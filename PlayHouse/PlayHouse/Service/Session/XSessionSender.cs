using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Session
{
    internal class XSessionSender : XSender, ISessionSender
    {
        private IClientCommunicator _clientCommunicator;

        public XSessionSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache):base(serviceId, clientCommunicator,reqCache)
        {
            this._clientCommunicator = clientCommunicator;
        }

        public void RelayToStage(string playEndpoint, long stageId, int sid, long accountId, ClientPacket packet)
        {
            var routePacket = RoutePacket.ApiOf(packet.ToRoutePacket(), false, false);
            routePacket.RouteHeader.StageId = stageId;
            routePacket.RouteHeader.AccountId = accountId;
            routePacket.RouteHeader.Header.MsgSeq = packet.MsgSeq;
            routePacket.RouteHeader.Sid = sid;
            routePacket.RouteHeader.IsToClient = false;
            _clientCommunicator.Send(playEndpoint, routePacket);
        }

        public void RelayToApi(string apiEndpoint, int sid, long accountId, ClientPacket packet)
        {
            var routePacket = RoutePacket.ApiOf( packet.ToRoutePacket(), false, false);
            routePacket.RouteHeader.Sid = sid;
            routePacket.RouteHeader.Header.MsgSeq = packet.MsgSeq;
            routePacket.RouteHeader.IsToClient = false;
            routePacket.RouteHeader.AccountId = accountId;

            _clientCommunicator.Send(apiEndpoint, routePacket);
        }
    }

   

}