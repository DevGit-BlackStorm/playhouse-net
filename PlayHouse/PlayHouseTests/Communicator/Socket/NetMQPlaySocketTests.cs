using FluentAssertions;
using Org.Ulalax.Playhouse.Protocol;
using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.Message.buffer;
using PlayHouse.Communicator.PlaySocket;
using Xunit;

namespace PlayHouse.Communicator.Socket.Tests
{

    [Collection("NetMQPlaySocketTests")]
    public class NetMQPlaySocketTests : IDisposable
    {

        
        private string serverBindEndpoint = "";
        private string clientBindEndpoint = "";

        private NetMQPlaySocket? serverSocket;
        private NetMQPlaySocket? clientSocket;

        public NetMQPlaySocketTests()
        {
            PooledBuffer.Init(1024 * 1024);

            string localIp = IpFinder.FindLocalIp();
            int serverPort = IpFinder.FindFreePort();
            int clientPort = IpFinder.FindFreePort();

            serverBindEndpoint = $"tcp://{localIp}:{serverPort}";
            clientBindEndpoint = $"tcp://{localIp}:{clientPort}";

            serverSocket = new NetMQPlaySocket(new SocketConfig(), serverBindEndpoint);
            clientSocket = new NetMQPlaySocket(new SocketConfig(), clientBindEndpoint);

            serverSocket.Bind();
            clientSocket.Bind();

            clientSocket.Connect(serverBindEndpoint);

            Thread.Sleep(200);
        }

        public void Dispose()
        {
            clientSocket!.Close();
            serverSocket!.Close();
        }

        [Fact]
        public void Send_Emtpy_Frame()
        {
            var sendRoutePacket = RoutePacket.Of(RouteHeader.Of(new HeaderMsg()), new EmptyPayload());
            clientSocket!.Send(serverBindEndpoint, sendRoutePacket);

            RoutePacket? recvPacket = null;
            while (recvPacket != null)
            {
                recvPacket = serverSocket!.Receive();
            }
            
        }

        [Fact]
        public void Send()
        {
            var message = new TestMsg
            {
                TestMsg_ = "Hello",
                TestNumber = 27
            };

            var header = new HeaderMsg
            {
                ErrorCode = 0,
                MsgSeq = 1,
                ServiceId = "session",
                MsgName = "TestMsg"
            };

            var routeHeader = RouteHeader.Of(header);

            var sendRoutePacket = RoutePacket.Of(routeHeader,new ProtoPayload(message));

            clientSocket!.Send(serverBindEndpoint, sendRoutePacket);

            RoutePacket? receiveRoutePacket = null;
            while (receiveRoutePacket == null)
            {
                receiveRoutePacket = serverSocket!.Receive();
            }


            receiveRoutePacket.RouteHeader.Header.ToMsg().Should().Be(header);
            receiveRoutePacket.RouteHeader.From.Should().Be(clientBindEndpoint);

            var receiveBody = TestMsg.Parser.ParseFrom(receiveRoutePacket.Data());

            receiveBody.Should().Be(message);           
        }

        
    }




}