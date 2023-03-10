
using PlayHouse.Communicator;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace PlayHouseTests.Communicator
{
    public class CacheTests : IAsyncLifetime
    {
        private readonly TestcontainerDatabase _testcontainers = new ContainerBuilder<RedisTestcontainer>().WithImage("redis:6.2.5").Build();

        private IConnectionMultiplexer? _connectionMultiplexer;

        readonly string endpoint1 = "127.0.0.1:8081";
        readonly string endpoint2 = "127.0.0.1:8081";

        public async Task InitializeAsync()
        {
            
            await _testcontainers.StartAsync();
            _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_testcontainers.ConnectionString);
        }

        public async Task DisposeAsync() => await _testcontainers.DisposeAsync().AsTask();

        [Fact]
        public void UpdateAndGet()
        {

            var redisClient = new RedisClient(_testcontainers.IpAddress, _testcontainers.Port);
            // Arrange
            //var cache = new RedisCache(_connectionMultiplexer!);

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
        public void TimeOver()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            XServerInfo serverInfo = new (endpoint1, ServiceType.SESSION, "session", ServerState.RUNNING, 0, 0);

            serverInfo.TimeOver().Should().BeFalse();

            serverInfo.LastUpdate = timestamp - 59000;

            serverInfo.TimeOver().Should().BeFalse();

            serverInfo.LastUpdate = timestamp - 61000;

            serverInfo.TimeOver().Should().BeTrue();
        }

      
    }




}
