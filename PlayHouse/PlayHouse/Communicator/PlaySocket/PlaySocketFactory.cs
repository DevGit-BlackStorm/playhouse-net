using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.PlaySocket;
internal abstract class PlaySocketFactory
{
    public static IPlaySocket CreatePlaySocket(SocketConfig config,string bindEndpoint)
    {
        return new NetMQPlaySocket(config, bindEndpoint);
    }
}
