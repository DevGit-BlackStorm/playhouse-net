﻿using FluentAssertions;
using Org.BouncyCastle.Utilities;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Service.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Moq;
using PlayHouse.Service.Session.network;
using Google.Protobuf.WellKnownTypes;

namespace PlayHouseTests.Service.Session
{
    public class SessionClientTest : IDisposable
    {
        private IServerInfoCenter _serviceCenter;
        private RequestCache _reqCache;
        private IClientCommunicator _clientCommunicator;
        private List<RoutePacket> _resultList;
        private List<string> _urls;
        private int _sid = 1;
        private ISession _session;

        private short _idSession = 1;
        private short _idApi = 2;
        

        public SessionClientTest()
        {
            _serviceCenter = Mock.Of<IServerInfoCenter>();
            _session = Mock.Of<ISession>();
            _reqCache = new RequestCache(0);
            _resultList = new List<RoutePacket>();
            _clientCommunicator = new SpyClientCommunicator(_resultList);
            _urls = new List<string>();
        }

        public void Dispose()
        {
            _resultList.Clear();
        }

        [Fact]
        public void WithoutAuthenticate_SendPacket_SocketShouldBeDisconnected()
        {
            var sessionClient = new SessionClient(_idSession, _sid, _serviceCenter, _session, _clientCommunicator, _urls, _reqCache);
            var clientPacket = new ClientPacket(new Header(serviceId:_idApi), new EmptyPayload());
            sessionClient.OnReceive(clientPacket);
            Mock.Get(_session).Verify(s => s.ClientDisconnect(), Moq.Times.Once());
        }

        [Fact]
        public void PacketOnTheAuthList_ShouldBeDelivered()
        {
            short messageId = 2;
            _urls.Add($"{_idApi}:{messageId}");

            Mock.Get(_serviceCenter)
                .Setup(s => s.FindRoundRobinServer(_idApi))
                .Returns(XServerInfo.Of("tcp://127.0.0.1:0021", ServiceType.API, _idApi, ServerState.RUNNING, 21, DateTimeOffset.Now.ToUnixTimeMilliseconds()));

            var sessionClient = new SessionClient(_idSession, _sid, _serviceCenter, _session, _clientCommunicator, _urls, _reqCache);
            var clientPacket = new ClientPacket(new Header(serviceId: _idApi,msgId:messageId), new EmptyPayload());
            sessionClient.OnReceive(clientPacket);

            _resultList.Should().HaveCount(1);
        }

        [Fact]
        public void ReceiveAuthenticatePacket_SessionClientShouldBeAuthenticated()
        {
            // api 서버로부터 authenticate 패킷을 받을 경우 인증 확인 및 session info 정보 확인
            long accountId = 1000L;
            string sessionInfo = "session infos";

            var message = new AuthenticateMsg()
            {
                ServiceId = _idApi,
                AccountId = accountId,
                SessionInfo = sessionInfo
            };
            var routePacket = RoutePacket.SessionOf(_sid, new Packet(message), true, true);

            var sessionClient = new SessionClient(_idSession, _sid, _serviceCenter, _session, _clientCommunicator, _urls, _reqCache);
            sessionClient.OnReceive(routePacket);

            sessionClient.IsAuthenticated.Should().BeTrue();
            sessionClient.GetSessionInfo(_idApi).Should().Be(sessionInfo);
        }

      
    }

}