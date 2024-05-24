using System.Net;
using System.Net.Sockets;
using PlayHouse.Communicator;
using Xunit;

namespace PlayHouseTests.Communicator;

public class IpFinderTest
{
    [Fact]
    public void FindLocalIp_ShouldBeValid()
    {
        Assert.True(IPAddress.TryParse(IpFinder.FindLocalIp(), out var localIp));
        Assert.Equal(AddressFamily.InterNetwork, localIp.AddressFamily);
    }

    [Fact]
    public void FindPublicIp_ShouldBeValid()
    {
        Assert.True(IPAddress.TryParse(IpFinder.FindPublicIp(), out var publicIp));
        Assert.Equal(AddressFamily.InterNetwork, publicIp.AddressFamily);
    }
}