﻿using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Xunit;
using FluentAssertions;
using PlayHouseConnector;
using Packet = PlayHouseConnector.Packet;
using CommonLib;

namespace PlayHouse.Service.Session.network
{
    public class SessionServerListener : ISessionListener
    {
        public bool UseWebSocket { get; set; }
        public RingBuffer Buffer { get; } = new RingBuffer(ConstOption.MAX_PACKET_SIZE);

        public string ResultValue { get; set; } = "";
        private ISession? _session;

        public void OnConnect(int sid, ISession session)
        {
            ResultValue = "onConnect";
            _session = session;
        }

        public void OnReceive(int sid, ClientPacket clientPacket)
        {
            var testMsg = TestMsg.Parser.ParseFrom(clientPacket.Data);

            if (testMsg.TestMsg_ == "request")
            {
                Buffer.Clear();
                RoutePacket.WriteClientPacketBytes(clientPacket, Buffer);

                int size = Buffer.Count;

                PooledBuffer buffer = new PooledBuffer(size);

                buffer.Append(Buffer.Buffer(), 0, size);
                clientPacket.Payload = new PooledBufferPayload(buffer);

                _session!.Send(clientPacket);
            }

            ResultValue = testMsg.TestMsg_;
        }

        public void OnDisconnect(int sid)
        {
            ResultValue = "onDisconnect";
        }
    }

    [Collection("SessionNetworkTest")]
    public class SessionNetworkTest
    {
        public SessionNetworkTest()
        {
            PooledBuffer.Init(1024 * 1024);

        }

        [Fact]
        public async Task ClientAndSessionCommunicate()
        {
            var useWebSocketArray = new bool[] { false,true};

            foreach (var useWebSocket in useWebSocketArray)
            {
                SessionServerListener serverListener = new (){ UseWebSocket = useWebSocket };
                var port = IpFinder.FindFreePort();

                var sessionNetwork = new SessionNetwork(new SessionOption { UseWebSocket = useWebSocket ,SessionPort = port}, serverListener);

                
                var serverThread = new Thread(() =>
                {
                    sessionNetwork.Start();
                    sessionNetwork.Await();
                    
                });
                serverThread.Start();

                await Task.Delay(100);


                var connector = new Connector(new ConnectorConfig() { ReqestTimeout = 0});

                var localIp = IpFinder.FindLocalIp();

                connector.Start();
                connector.Connect(localIp, port);

                await Task.Delay(100);
                serverListener.ResultValue.Should().Be("onConnect");

                connector.Send((short)ServiceType.API, new Packet(new TestMsg { TestMsg_ = "test" }));

                await Task.Delay(100);


                var replyPacket = await connector.Request(1, new Packet(new TestMsg { TestMsg_ = "request" }));

                using (replyPacket)
                {
                    TestMsg.Parser.ParseFrom(replyPacket.Data).TestMsg_.Should().Be("request");
                }
                

                replyPacket = await connector.Request(1, new Packet(new TestMsg { TestMsg_ = "request" }));

                using (replyPacket)
                {
                    TestMsg.Parser.ParseFrom(replyPacket.Data).TestMsg_.Should().Be("request");
                }
                
                connector.Disconnect();

                await Task.Delay(100);
                serverListener.ResultValue.Should().Be("onDisconnect");

                sessionNetwork.Stop();
            }
        }
    }

  
}