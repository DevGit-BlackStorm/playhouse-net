using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production
{

    public class CommonOption
    {
        public int Port { get; set; }
        public string RedisIp { get; } = "localhost";
        public int RedisPort { get; set; } = 6379;

        public ushort ServiceId { get; set; } = ConstOption.DefaultServiceId;
        public ServerSystemFactory? ServerSystem { get; set; }
        public int RequestTimeoutSec { get; set; } = 5;
        public bool ShowQps { get; set; }

        public int MaxBufferPoolSize = 1024 * 1024 * 100;

    }
}
