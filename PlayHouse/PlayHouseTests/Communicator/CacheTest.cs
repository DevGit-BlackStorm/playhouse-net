
using PlayHouse.Communicator;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;
using Testcontainers.Redis;

namespace PlayHouseTests.Communicator
{
    public class CacheTests : IAsyncLifetime
    {
        const int port = 6379;

        private readonly RedisContainer _redisContainer = new RedisBuilder().WithExposedPort(port).Build();
                

        readonly string endpoint1 = "127.0.0.1:8081";
        readonly string endpoint2 = "127.0.0.1:8082";

        public async Task InitializeAsync()
        {
             await _redisContainer.StartAsync();

        }

        public async Task DisposeAsync() => await _redisContainer.DisposeAsync().AsTask();

        [Fact]
        public void Test_ServerInfo_Update_And_Get()
        {

            // Arrange
            RedisStorageClient redisClient = new RedisStorageClient(_redisContainer.Hostname, _redisContainer.GetMappedPublicPort(port));
            redisClient.Connect();


            // act
            redisClient.UpdateServerInfo(new XServerInfo(endpoint1,ServiceType.SESSION,"session",ServerState.RUNNING,0,0));
            redisClient.UpdateServerInfo(new XServerInfo(endpoint2, ServiceType.API, "api", ServerState.RUNNING, 0, 0));

            // Assert
            List<XServerInfo> serverList = redisClient.GetServerList("");

            serverList.Count.Should().Be(2);
            serverList[0].State.Should().Be(ServerState.RUNNING);
            serverList.Should().Contain(s => s.BindEndpoint == endpoint1)
                 .And.Contain(s => s.BindEndpoint == endpoint2);

        }


        [Fact]
        public void Test_TimeOver()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            XServerInfo serverInfo = new (endpoint1, ServiceType.SESSION, "session", ServerState.RUNNING, 0, timestamp);

            serverInfo.TimeOver().Should().BeFalse();

            serverInfo.LastUpdate = timestamp - 59000;

            serverInfo.TimeOver().Should().BeFalse();

            serverInfo.LastUpdate = timestamp - 61000;

            serverInfo.TimeOver().Should().BeTrue();
        }

      
    }




}
