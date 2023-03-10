using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    public interface IClientCommunicator
    {
        void Connect(string endpoint);
        void Send(string endpoint, RoutePacket routePacket);
        void Communicate();
        void Disconnect(string endpoint);
        void Stop();

  
    }
}
