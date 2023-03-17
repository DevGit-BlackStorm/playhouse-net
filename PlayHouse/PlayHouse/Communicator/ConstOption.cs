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
    }
}
