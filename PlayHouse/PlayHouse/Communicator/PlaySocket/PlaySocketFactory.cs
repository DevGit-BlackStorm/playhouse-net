using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.PlaySocket;
internal abstract class PlaySocketFactory
{
    public static IPlaySocket CreatePlaySocket(SocketConfig config, String id)
    {
        return new NetMQPlaySocket(config, id);
    }
}
