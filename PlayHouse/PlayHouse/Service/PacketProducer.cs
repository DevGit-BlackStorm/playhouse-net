using PlayHouse.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    static class PacketProducer
    {
        public static Func<IPacket, IPacket>? Create { get; set; }
    }
}
