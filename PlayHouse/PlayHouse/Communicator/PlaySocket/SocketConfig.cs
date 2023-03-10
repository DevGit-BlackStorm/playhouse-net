using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.PlaySocket
{
    public class SocketConfig
    {
        public int BackLog { get; internal set; } = 1000;
        public int Linger { get; internal set; } = 0;
        public int SendBufferSize { get; internal set; } = 1024 * 1024;
        public int ReceiveBufferSize { get; internal set;} = 1024 * 1024;
        public int SendHighWatermark { get; internal set; } = 1000000;
        public int ReceiveHighWatermark { get; internal set; } = 1000000;
    }
}
