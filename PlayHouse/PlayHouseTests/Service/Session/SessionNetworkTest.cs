using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Xunit;
using FluentAssertions;
using PlayHouseConnector;
using CommonLib;
using PlayHouse.Production.Session;
using NetMQ;

namespace PlayHouse.Service.Session.Network
{
    internal class SessionServerListener : ISessionListener
    {
        public bool UseWebSocket { get; set; }
        public PooledByteBuffer Buffer { get; } = new PooledByteBuffer(ConstOption.MaxPacketSize);

        public string ResultValue { get; set; } = "";
        private ISession? _session;

        public void OnConnect(int sid, ISession session)
        {
            ResultValue = "onConnect";
            _session = session;
        }

        public void OnReceive(int sid, ClientPacket clientPacket)
        {
            Console.WriteLine($"OnReceive sid:{sid},packetInfo:{clientPacket.Header}");
            var testMsg = TestMsg.Parser.ParseFrom(clientPacket.Span);

            if (testMsg.TestMsg_ == "request")
            {
                Buffer.Clear();
                clientPacket.Header.ErrorCode = 0;
                RoutePacket.WriteClientPacketBytes(clientPacket, Buffer);

                NetMQFrame frame = new NetMQFrame(Buffer.Buffer(), Buffer.Count);
                clientPacket.Payload = new FramePayload(frame);

                

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
            const ushort SESSION = 1;
            const ushort API = 2;


            var useWebSocketArray = new bool[] { false, true };

            foreach (var useWebSocket in useWebSocketArray)
            {
                SessionServerListener serverListener = new() { UseWebSocket = useWebSocket };
                var port = IpFinder.FindFreePort();

                var sessionNetwork = new SessionNetwork(new SessionOption { UseWebSocket = useWebSocket, SessionPort = port }, serverListener);

                var serverThread = new Thread(() =>
                {
                    sessionNetwork.Start();
                    sessionNetwork.Await();

                });
                serverThread.Start();

                await Task.Delay(100);

                var localIp = IpFinder.FindLocalIp();
                var connector = new Connector();
                connector.Init(new ConnectorConfig() { RequestTimeoutMs = 0 ,Host = localIp,Port = port});
                

                Timer timer = new Timer((task) =>
                {
                    connector.MainThreadAction();    
                }, null, 0, 10);
                
                
                connector.Connect();

                await Task.Delay(100);
                serverListener.ResultValue.Should().Be("onConnect");

                

                var replyPacket = await connector.AuthenticateAsync(SESSION, new PlayHouseConnector.Packet(new TestMsg { TestMsg_ = "request" }));

                using (replyPacket)
                {
                    TestMsg.Parser.ParseFrom(replyPacket.Data).TestMsg_.Should().Be("request");
                }

                connector.Send(API, new PlayHouseConnector.Packet(new TestMsg { TestMsg_ = "test" }));
                await Task.Delay(100);



                replyPacket = await connector.RequestAsync(SESSION, new PlayHouseConnector.Packet(new TestMsg { TestMsg_ = "request" }));

                using (replyPacket)
                {
                    TestMsg.Parser.ParseFrom(replyPacket.Data).TestMsg_.Should().Be("request");
                }

                connector.Disconnect();

                await Task.Delay(100);
                serverListener.ResultValue.Should().Be("onDisconnect");

                sessionNetwork.Stop();
                await timer.DisposeAsync();

            }
        }
    }

  
}
