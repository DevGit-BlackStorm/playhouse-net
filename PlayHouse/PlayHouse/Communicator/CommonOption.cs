using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    
    public class CommonOption
    {
        public int Port { get; set; }
        public string RedisIp { get; } = "localhost";
        public int RedisPort { get; set; } = 6379;

        public short ServiceId { get; set; } = ConstOption.DEFAULT_SERVICE_ID;
        public ServerSystemFactory? ServerSystem { get; set; }
        public int RequestTimeoutSec { get; set; } = 5;
        public bool ShowQps { get; set; }

    }
}
