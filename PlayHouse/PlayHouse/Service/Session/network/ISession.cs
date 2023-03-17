using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Session.network
{
    public interface ISession
    {
        void ClientDisconnect();
        void Send(ClientPacket packet);
    }
}
