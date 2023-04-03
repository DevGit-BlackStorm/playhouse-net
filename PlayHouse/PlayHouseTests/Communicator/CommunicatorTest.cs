using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using Playhouse.Protocol;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse;
using Xunit;
using FluentAssertions;
using CommonLib;

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
        public CommunicatorTest() { 
            PooledBuffer.Init();
        }
        [Fact]
        public void Should_communicate_between_Session_and_Api()
        {
            var localIp = IpFinder.FindLocalIp();

            var sessionPort = IpFinder.FindFreePort();
            var sessionEndpoint = $"tcp://{localIp}:{sessionPort}";
            var sessionServer = new XServerCommunicator(new NetMQPlaySocket(new SocketConfig(), sessionEndpoint));
            var sessionClient = new XClientCommunicator(new NetMQPlaySocket(new SocketConfig(), sessionEndpoint));

            var sessionListener = new TestListener();
            sessionServer.Bind(sessionListener);

            var apiPort = IpFinder.FindFreePort();
            var apiEndpoint = $"tcp://{localIp}:{apiPort}";
            var apiServer = new XServerCommunicator(new NetMQPlaySocket(new SocketConfig(), apiEndpoint));
            var apiClient = new XClientCommunicator(new NetMQPlaySocket(new SocketConfig(), apiEndpoint));

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

            Thread.Sleep(100);

            var message = new HeaderMsg();
            sessionClient.Send(apiEndpoint, RoutePacket.ClientOf((short)ServiceType.SESSION, 0, new Packet(message)));

            Thread.Sleep(200);

            apiListener.Results.Count.Should().Be(1);
            apiListener.Results[0].GetMsgId().Should().Be((short)HeaderMsg.Descriptor.Index);

            ////////// api to session ///////////////

            apiClient.Connect(sessionEndpoint);
            sessionListener.Results.Clear();

            Thread.Sleep(100);

            short messagId = 100;
            apiClient.Send(sessionEndpoint, RoutePacket.ClientOf((short)ServiceType.API, 0, new Packet(messagId)));

            Thread.Sleep(200);

            sessionListener.Results.Count.Should().Be(1);
            sessionListener.Results[0].GetMsgId().Should().Be(messagId);

        }
    }

}
