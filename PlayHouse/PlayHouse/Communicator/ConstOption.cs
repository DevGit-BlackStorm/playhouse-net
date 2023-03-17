using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    public static class ConstOption
    {
        public const int ThreadSleep = 50;

        public static int ADDRESS_RESOLVER_INITIAL_DELAY { get; internal set; } = 1000;
        public static int ADDRESS_RESOLVER_PERIOD { get; internal set; } = 3000;
        public const int SessionBufferSize = 4 * 1024;

        public const int MAX_PACKET_SIZE = 65535;
        public const int HEADER_SIZE = 256;
        public const int LENGTH_FIELD_SIZE = 3;
    }
}
