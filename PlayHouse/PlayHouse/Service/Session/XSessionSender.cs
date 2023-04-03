using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Session
{
    public class XSessionSender : BaseSender, ISessionSender
    {
        private short serviceId;
        private IClientCommunicator clientCommunicator;
        private RequestCache reqCache;

        public XSessionSender(short serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache):base(serviceId, clientCommunicator,reqCache)
        {
            this.serviceId = serviceId;
            this.clientCommunicator = clientCommunicator;
            this.reqCache = reqCache;
        }

        public void RelayToRoom(string playEndpoint, long stageId, int sid, long accountId, string sessionInfo, ClientPacket packet, short msgSeq)
        {
            var routePacket = RoutePacket.ApiOf(sessionInfo, packet.ToPacket(), false, false);
            routePacket.RouteHeader.StageId = stageId;
            routePacket.RouteHeader.AccountId = accountId;
            routePacket.RouteHeader.Header.MsgSeq = msgSeq;
            routePacket.RouteHeader.Sid = sid;
            routePacket.RouteHeader.ForClient = true;
            clientCommunicator.Send(playEndpoint, routePacket);
        }

        public void RelayToApi(string apiEndpoint, int sid, string sessionInfo, ClientPacket packet, short msgSeq)
        {
            var routePacket = RoutePacket.ApiOf(sessionInfo, packet.ToPacket(), false, false);
            routePacket.RouteHeader.Sid = sid;
            routePacket.RouteHeader.Header.MsgSeq = msgSeq;
            routePacket.RouteHeader.ForClient = true;
            clientCommunicator.Send(apiEndpoint, routePacket);
        }
    }

   

}