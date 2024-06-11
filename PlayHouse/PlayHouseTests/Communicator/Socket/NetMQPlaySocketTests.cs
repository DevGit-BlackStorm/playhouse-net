using CommonLib;
using FluentAssertions;
using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.PlaySocket;
using Playhouse.Protocol;
using Xunit;

namespace PlayHouse.Communicator.Socket.Tests;

[Collection("NetMQPlaySocketTests")]
public class NetMQPlaySocketTests : IDisposable
{
    private readonly string clientBindEndpoint = "";
    private readonly NetMqPlaySocket? clientSocket;

    private readonly string serverBindEndpoint = "";

    private readonly NetMqPlaySocket? serverSocket;

    public NetMQPlaySocketTests()
    {
        PooledBuffer.Init(1024 * 1024);

        var localIp = IpFinder.FindLocalIp();
        var serverPort = IpFinder.FindFreePort();
        var clientPort = IpFinder.FindFreePort();

        serverBindEndpoint = $"tcp://{localIp}:{serverPort}";
        clientBindEndpoint = $"tcp://{localIp}:{clientPort}";

        serverSocket = new NetMqPlaySocket(new SocketConfig(), serverBindEndpoint);
        clientSocket = new NetMqPlaySocket(new SocketConfig(), clientBindEndpoint);

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
            ErrorCode = 10,
            MsgSeq = 1,
            ServiceId = (short)ServiceType.SESSION,
            MsgId = TestMsg.Descriptor.Name
        };

        var routeHeader = RouteHeader.Of(header);

        var sendRoutePacket = RoutePacket.Of(routeHeader, new ProtoPayload(message));


        clientSocket!.Send(serverBindEndpoint, sendRoutePacket);

        RoutePacket? receiveRoutePacket = null;
        while (receiveRoutePacket == null)
        {
            receiveRoutePacket = serverSocket!.Receive();
            Thread.Sleep(10);
        }


        receiveRoutePacket.RouteHeader.Header.ToMsg().Should().Be(header);
        receiveRoutePacket.RouteHeader.From.Should().Be(clientBindEndpoint);

        var receiveBody = TestMsg.Parser.ParseFrom(receiveRoutePacket.Span);

        receiveBody.Should().Be(message);
    }
}