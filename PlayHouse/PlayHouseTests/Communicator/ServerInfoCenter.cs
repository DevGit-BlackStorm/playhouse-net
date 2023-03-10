using FluentAssertions;
using PlayHouse;
using PlayHouse.Communicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PlayHouseTests.Communicator
{
    public class ServerInfoCenterFuncSpecTest
    {
        private XServerInfoCenter serverInfoCenter;
        private long curTime;
        private List<XServerInfo> serverList;

        public ServerInfoCenterFuncSpecTest()
        {
            serverInfoCenter = new XServerInfoCenter(new ConsoleLogger());
            curTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            serverList = new List<XServerInfo>
            {
                XServerInfo.Of("tcp://127.0.0.1:0001", ServiceType.API, "api", ServerState.RUNNING, 1, curTime),
                XServerInfo.Of("tcp://127.0.0.1:0002", ServiceType.Play, "play", ServerState.RUNNING, 1, curTime),
                XServerInfo.Of("tcp://127.0.0.1:0003", ServiceType.SESSION, "session", ServerState.RUNNING, 1, curTime),

                XServerInfo.Of("tcp://127.0.0.1:0011", ServiceType.API, "api", ServerState.RUNNING, 11, curTime),
                XServerInfo.Of("tcp://127.0.0.1:0012", ServiceType.Play, "play", ServerState.RUNNING, 11, curTime),
                XServerInfo.Of("tcp://127.0.0.1:0013", ServiceType.SESSION, "session", ServerState.RUNNING, 11, curTime),

                XServerInfo.Of("tcp://127.0.0.1:0021", ServiceType.API, "api", ServerState.RUNNING, 21, curTime),
                XServerInfo.Of("tcp://127.0.0.1:0022", ServiceType.Play, "play", ServerState.RUNNING, 21, curTime),
                XServerInfo.Of("tcp://127.0.0.1:0023", ServiceType.SESSION, "session", ServerState.RUNNING, 21, curTime)
            };
        }

        [Fact]
        public void RemoveInvalidServerInfoFromTheList()
        {
            var updatedList = serverInfoCenter.Update(serverList);

            updatedList.Should().HaveCount(serverList.Count);

            var update = new List<XServerInfo>{
                XServerInfo.Of("tcp://127.0.0.1:0001", ServiceType.API, "api", ServerState.DISABLE, 11, curTime)
            };

            updatedList = serverInfoCenter.Update(update);

            updatedList.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveTimedOutServerInfoFromTheList()
        {
            serverInfoCenter.Update(serverList);

            var update = new List<XServerInfo>{
                XServerInfo.Of("tcp://127.0.0.1:0011", ServiceType.API, "api", ServerState.RUNNING, 1, curTime - 61000)
            };

            var updatedList = serverInfoCenter.Update(update);

            updatedList.Should().HaveCount(1);
            updatedList[0].State.Should().Be(ServerState.DISABLE);
        }

        [Fact]
        public void ReturnTheCorrectServerInfoWhenSearchingForAnExistingServer()
        {
            serverInfoCenter.Update(serverList);

            var findServerEndpoint = "tcp://127.0.0.1:0021";
            var serverInfo = serverInfoCenter.FindServer(findServerEndpoint);

            serverInfo.BindEndpoint.Should().Be(findServerEndpoint);
            serverInfo.State.Should().Be(ServerState.RUNNING);

            System.Action act = () => serverInfoCenter.FindServer("");
            act.Should().Throw<CommunicatorException.NotExistServerInfo>();
        }



        [Fact]
        public void ReturnTheCorrectRoundRobinServerInfo()
        {
            serverInfoCenter.Update(serverList);

            // Play service should return servers in order 0012 -> 0022 -> 0002
            serverInfoCenter.FindRoundRobinServer("play").BindEndpoint.Should().Be("tcp://127.0.0.1:0012");
            serverInfoCenter.FindRoundRobinServer("play").BindEndpoint.Should().Be("tcp://127.0.0.1:0022");
            serverInfoCenter.FindRoundRobinServer("play").BindEndpoint.Should().Be("tcp://127.0.0.1:0002");

            // Session service should return servers in order 0013 -> 0023 -> 0003
            serverInfoCenter.FindRoundRobinServer("session").BindEndpoint.Should().Be("tcp://127.0.0.1:0013");
            serverInfoCenter.FindRoundRobinServer("session").BindEndpoint.Should().Be("tcp://127.0.0.1:0023");
            serverInfoCenter.FindRoundRobinServer("session").BindEndpoint.Should().Be("tcp://127.0.0.1:0003");
        }

        [Fact]
        public void ReturnTheFullListOfServerInfo()
        {
            serverInfoCenter.Update(serverList);
            serverInfoCenter.GetServerList().Should().HaveCount(9);
        }
    }
}
