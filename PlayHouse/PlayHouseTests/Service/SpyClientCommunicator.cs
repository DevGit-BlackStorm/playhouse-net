using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouseTests.Service
{
    public class SpyClientCommunicator : IClientCommunicator
    {
        private List<RoutePacket> _resultCollector;
        public SpyClientCommunicator(List<RoutePacket> resultCollector) 
        { 
            _resultCollector = resultCollector;
        }
        public void Communicate()
        {
        }

        public void Connect(string endpoint)
        {
        }

        public void Disconnect(string endpoint)
        {
        }

        public void Send(string endpoint, RoutePacket routePacket)
        {
            _resultCollector.Add(routePacket);
        }

        public void Stop()
        {
        }
    }
}
