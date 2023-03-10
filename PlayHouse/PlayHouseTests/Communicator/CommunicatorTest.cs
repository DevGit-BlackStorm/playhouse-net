using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse;
using Xunit;
using FluentAssertions;

namespace PlayHouseTests.Communicator
{
    public class TestListener : ICommunicateListener
    {
        public List<RoutePacket> Results = new();
        public void OnReceive(RoutePacket routePacket)
        {
            Results.Add(routePacket);
        }
    }


    [Collection("ZSocketCommunicateTest")]
    public class CommunicatorTest
    {
        [Fact]
        public void Communicate()
        {
            var localIp = IpFinder.FindLocalIp();

            var sessionPort = IpFinder.FindFreePort();
            var sessionEndpoint = $"tcp://{localIp}:{sessionPort}";
            var sessionServer = new XServerCommunicator(new NetMQPlaySocket(new SocketConfig(), sessionEndpoint, new ConsoleLogger()), new ConsoleLogger());
            var sessionClient = new XClientCommunicator(new NetMQPlaySocket(new SocketConfig(), sessionEndpoint, new ConsoleLogger()), new ConsoleLogger());

            var sessionListener = new TestListener();
            sessionServer.Bind(sessionListener);

            var apiPort = IpFinder.FindFreePort();
            var apiEndpoint = $"tcp://{localIp}:{apiPort}";
            var apiServer = new XServerCommunicator(new NetMQPlaySocket(new SocketConfig(), apiEndpoint, new ConsoleLogger()), new ConsoleLogger());
            var apiClient = new XClientCommunicator(new NetMQPlaySocket(new SocketConfig(), apiEndpoint, new ConsoleLogger()), new ConsoleLogger());

            var apiListener = new TestListener();
            apiServer.Bind(apiListener);

            Thread sessionServerThread = new Thread(() =>
            {
                sessionServer.Communicate();
            });

            Thread sessionClientThread = new Thread(() =>
            {
                sessionClient.Communicate();
            });

            Thread apiServerThread = new Thread(() =>
            {
                apiServer.Communicate();
            });

            Thread apiClientThread = new Thread(() =>
            {
                apiClient.Communicate();
            });

            sessionServerThread.Start();
            sessionClientThread.Start();
            apiServerThread.Start();
            apiClientThread.Start();

            ///////// session to api ///////////

            sessionClient.Connect(apiEndpoint);
            apiListener.Results.Clear();

            var message = new HeaderMsg() { MsgName = "sessionPacket" };
            sessionClient.Send(apiEndpoint, RoutePacket.ClientOf("session", 0, new Packet(message)));

            apiListener.Results.Count.Should().Be(1);
            apiListener.Results[0].MsgName().Should().Be("HeaderMsg");

            ////////// api to session ///////////////

            apiClient.Connect(sessionEndpoint);
            sessionListener.Results.Clear();

            apiClient.Send(sessionEndpoint, RoutePacket.ClientOf("api", 0, new Packet("apiPacket")));

            apiListener.Results.Count.Should().Be(1);
            apiListener.Results[0].MsgName().Should().Be("apiPacket");

        }
    }

}
