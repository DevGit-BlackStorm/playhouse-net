using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.PlaySocket
{
    public class PlaySocketFactory
    {
        static public IPlaySocket CreatePlaySocket(SocketConfig config, String id)
        {
            return new NetMQPlaySocket(config, id);
        }
    }
}
