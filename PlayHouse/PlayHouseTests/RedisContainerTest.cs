using Testcontainers.Redis;
using Xunit;

namespace PlayHouseTests;

public class RedisContainerTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder().Build();

    public Task DisposeAsync()
    {
        return _redisContainer.DisposeAsync().AsTask();
    }

    public Task InitializeAsync()
    {
        return _redisContainer.StartAsync();
    }
}

[CollectionDefinition("Redis")]
public class RedisCollection : ICollectionFixture<RedisContainerTest>
{
    // 이 클래스는 멤버를 가질 필요가 없습니다.
    // 단지 RedisContainerTest를 사용하는 테스트 클래스를 그룹화하는 역할을 합니다.
}
