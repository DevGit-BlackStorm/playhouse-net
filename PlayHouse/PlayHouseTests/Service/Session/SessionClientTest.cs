using FluentAssertions;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Service.Session;
using Xunit;
using Moq;
using PlayHouse.Service.Session.Network;
using PlayHouse.Production;
using Google.Protobuf;

namespace PlayHouseTests.Service.Session
{
    public class SessionClientTest : IDisposable
    {
        private IServerInfoCenter _serviceCenter;
        private RequestCache _reqCache;
        private IClientCommunicator _clientCommunicator;
        private List<string> _urls;
        private int _sid = 1;
        private ISession _session;

        private ushort _idSession = 1;
        private ushort _idApi = 2;
        

        public SessionClientTest()
        {
            _serviceCenter = new XServerInfoCenter();

            _serviceCenter.Update(new List<XServerInfo>
            { XServerInfo.Of("tcp://127.0.0.1:0021", ServiceType.API, _idApi, ServerState.RUNNING, 21, DateTimeOffset.Now.ToUnixTimeMilliseconds()) });

            _session = Mock.Of<ISession>();
            _reqCache = new RequestCache(0);
            _clientCommunicator = Mock.Of<IClientCommunicator>();
            _urls = new List<string>();
        }

        public void Dispose()
        {
        }

        [Fact]
        public void WithoutAuthenticate_SendPacket_SocketShouldBeDisconnected()
        {
            var sessionClient = new SessionClient(_idSession, _sid, _serviceCenter, _session, _clientCommunicator, _urls, _reqCache);
            var clientPacket = new ClientPacket(new Header(serviceId:_idApi), new EmptyPayload());
            sessionClient.Dispatch(clientPacket);
            Mock.Get(_session).Verify(s => s.ClientDisconnect(), Moq.Times.Once());
        }

        [Fact]
        public void PacketOnTheAuthList_ShouldBeDelivered()
        {
            short messageId = 2;
            _urls.Add($"{_idApi}:{messageId}");
            
            var sessionClient = new SessionClient(_idSession, _sid, _serviceCenter, _session, _clientCommunicator, _urls, _reqCache);
            var clientPacket = new ClientPacket(new Header(serviceId: _idApi,msgId:messageId), new EmptyPayload());
            sessionClient.Dispatch(clientPacket);

            Mock.Get(_clientCommunicator).Verify(c => c.Send(It.IsAny<string>(),It.IsAny<RoutePacket>()),Times.Once());
        }

        [Fact]
        public void ReceiveAuthenticatePacket_SessionClientShouldBeAuthenticated()
        {
            // api 서버로부터 authenticate 패킷을 받을 경우 인증 확인 및 session info 정보 확인
            //long accountId = 1000L;
            Guid accountId = Guid.NewGuid();

            var message = new AuthenticateMsg()
            {
                ServiceId = _idApi,
                AccountId = ByteString.CopyFrom(accountId.ToByteArray()),
            };
            var routePacket = RoutePacket.SessionOf(_sid, new Packet(message), true, true);

            var sessionClient = new SessionClient(_idSession, _sid, _serviceCenter, _session, _clientCommunicator, _urls, _reqCache);
            sessionClient.Dispatch(routePacket);

            sessionClient.IsAuthenticated.Should().BeTrue();
        }

      
    }

}
