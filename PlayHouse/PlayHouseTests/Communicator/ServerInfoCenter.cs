using FluentAssertions;
using PlayHouse;
using PlayHouse.Communicator;
using PlayHouse.Production;
using Xunit;

namespace PlayHouseTests.Communicator
{

    public class ServerInfoCenterFuncSpecTest
    {
        private XServerInfoCenter _serverInfoCenter;
        private long _curTime;
        private List<XServerInfo> _serverList;



        public ServerInfoCenterFuncSpecTest()
        {
            _serverInfoCenter = new XServerInfoCenter();
            _curTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _serverList = new List<XServerInfo>
            {
                XServerInfo.Of("tcp://127.0.0.1:0001", ServiceType.API, (ushort)ServiceType.API, ServerState.RUNNING, 1, _curTime),
                XServerInfo.Of("tcp://127.0.0.1:0002", ServiceType.Play, (ushort)ServiceType.Play, ServerState.RUNNING, 1, _curTime),
                XServerInfo.Of("tcp://127.0.0.1:0003", ServiceType.SESSION, (ushort)ServiceType.SESSION, ServerState.RUNNING, 1, _curTime),

                XServerInfo.Of("tcp://127.0.0.1:0011", ServiceType.API, (ushort)ServiceType.API, ServerState.RUNNING, 11, _curTime),
                XServerInfo.Of("tcp://127.0.0.1:0012", ServiceType.Play, (ushort)ServiceType.Play, ServerState.RUNNING, 11, _curTime),
                XServerInfo.Of("tcp://127.0.0.1:0013", ServiceType.SESSION, (ushort)ServiceType.SESSION, ServerState.RUNNING, 11, _curTime),

                XServerInfo.Of("tcp://127.0.0.1:0021", ServiceType.API, (ushort)ServiceType.API, ServerState.RUNNING, 21, _curTime),
                XServerInfo.Of("tcp://127.0.0.1:0022", ServiceType.Play, (ushort)ServiceType.Play, ServerState.RUNNING, 21, _curTime),
                XServerInfo.Of("tcp://127.0.0.1:0023", ServiceType.SESSION, (ushort)ServiceType.SESSION, ServerState.RUNNING, 21, _curTime)
            };
        }

        [Fact]
        public void RemoveInvalidServerInfoFromTheList()
        {
            var updatedList = _serverInfoCenter.Update(_serverList);

            updatedList.Should().HaveCount(_serverList.Count);

            var update = new List<XServerInfo>{
                XServerInfo.Of("tcp://127.0.0.1:0001", ServiceType.API, (ushort)ServiceType.API, ServerState.DISABLE, 11, _curTime)
            };

            updatedList = _serverInfoCenter.Update(update);

            updatedList.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveTimedOutServerInfoFromTheList()
        {
            _serverInfoCenter.Update(_serverList);

            var update = new List<XServerInfo>{
                XServerInfo.Of("tcp://127.0.0.1:0011", ServiceType.API, (ushort)ServiceType.API, ServerState.RUNNING, 1, _curTime - 61000)
            };

            var updatedList = _serverInfoCenter.Update(update);

            updatedList.Should().HaveCount(1);
            updatedList[0].State.Should().Be(ServerState.DISABLE);
        }

        [Fact]
        public void ReturnTheCorrectServerInfoWhenSearchingForAnExistingServer()
        {
            _serverInfoCenter.Update(_serverList);

            var findServerEndpoint = "tcp://127.0.0.1:0021";
            var serverInfo = _serverInfoCenter.FindServer(findServerEndpoint);

            serverInfo.BindEndpoint.Should().Be(findServerEndpoint);
            serverInfo.State.Should().Be(ServerState.RUNNING);

            System.Action act = () => _serverInfoCenter.FindServer("");
            act.Should().Throw<CommunicatorException.NotExistServerInfo>();
        }



        [Fact]
        public void ReturnTheCorrectRoundRobinServerInfo()
        {
            _serverInfoCenter.Update(_serverList);

            // Play service should return servers in order 0012 -> 0022 -> 0002
            _serverInfoCenter.FindRoundRobinServer((ushort)ServiceType.Play).BindEndpoint.Should().Be("tcp://127.0.0.1:0012");
            _serverInfoCenter.FindRoundRobinServer((ushort)ServiceType.Play).BindEndpoint.Should().Be("tcp://127.0.0.1:0022");
            _serverInfoCenter.FindRoundRobinServer((ushort)ServiceType.Play).BindEndpoint.Should().Be("tcp://127.0.0.1:0002");

            // Session service should return servers in order 0013 -> 0023 -> 0003
            _serverInfoCenter.FindRoundRobinServer((ushort)ServiceType.SESSION).BindEndpoint.Should().Be("tcp://127.0.0.1:0013");
            _serverInfoCenter.FindRoundRobinServer((ushort)ServiceType.SESSION).BindEndpoint.Should().Be("tcp://127.0.0.1:0023");
            _serverInfoCenter.FindRoundRobinServer((ushort)ServiceType.SESSION).BindEndpoint.Should().Be("tcp://127.0.0.1:0003");
        }

        [Fact]
        public void ReturnTheFullListOfServerInfo()
        {
            _serverInfoCenter.Update(_serverList);
            _serverInfoCenter.GetServerList().Should().HaveCount(9);
        }
    }
}
