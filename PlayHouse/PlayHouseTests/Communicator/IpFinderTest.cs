using PlayHouse.Communicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PlayHouseTests.Communicator
{
    public class IpFinderTest
    {
        [Fact]
        public void FindLocalIp_ShouldBeValid()
        {
            Assert.True(IPAddress.TryParse(IpFinder.FindLocalIp(), out IPAddress? localIp));
            Assert.Equal(AddressFamily.InterNetwork, localIp.AddressFamily);
        }

        [Fact]
        public void FindPublicIp_ShouldBeValid()
        {
            Assert.True(IPAddress.TryParse(IpFinder.FindPublicIp(), out IPAddress? publicIp));
            Assert.Equal(AddressFamily.InterNetwork, publicIp.AddressFamily);
        }
    }
}
