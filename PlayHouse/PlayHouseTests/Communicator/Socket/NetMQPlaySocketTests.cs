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
    public class NetMQPlaySocketTests
    {

        private string localIp = IpFinder.FindLocalIp();
        private int serverPort = IpFinder.FindFreePort();
        private int clientPort = IpFinder.FindFreePort();

        private string serverBindEndpoint = "";
        private string clientBindEndpoint = "";

        private NetMQPlaySocket? serverSocket;
        private NetMQPlaySocket? clientSocket;

        public NetMQPlaySocketTests()
        {
            PooledBuffer.Init(1024 * 1024);

            serverBindEndpoint = $"tcp://{localIp}:{serverPort}";
            clientBindEndpoint = $"tcp://{localIp}:{clientPort}";

            serverSocket = new NetMQPlaySocket(new SocketConfig(), serverBindEndpoint, new ConsoleLogger());
            clientSocket = new NetMQPlaySocket(new SocketConfig(), clientBindEndpoint, new ConsoleLogger());

            serverSocket.Bind();
            clientSocket.Bind();

            clientSocket.Connect(serverBindEndpoint);
        }

        [Fact]
        public void Send_Emtpy_Frame()
        {
            var sendRoutePacket = RoutePacket.Of(RouteHeader.Of(new HeaderMsg()), Payload.Empty());
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

            var sendRoutePacket = RoutePacket.Of(routeHeader, message);

            clientSocket!.Send(serverBindEndpoint, sendRoutePacket);

            RoutePacket? receiveRoutePacket = null;
            while (receiveRoutePacket == null)
            {
                receiveRoutePacket = serverSocket!.Receive();
            }


            receiveRoutePacket.RouteHeader.Header.ToMsg().Should().Be(message);
            receiveRoutePacket.RouteHeader.From.Should().Be(clientBindEndpoint);

            var receiveBody = TestMsg.Parser.ParseFrom(receiveRoutePacket.Data());

            receiveBody.Should().Be(message);

            clientSocket!.Close();
            serverSocket!.Close();
        }
    }




}