using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Session.network
{
    public interface ISessionListener
    {
        void OnConnect(int sid, ISession session);
        void OnReceive(int sid, ClientPacket clientPacket);
        void OnDisconnect(int sid);

    }
}
