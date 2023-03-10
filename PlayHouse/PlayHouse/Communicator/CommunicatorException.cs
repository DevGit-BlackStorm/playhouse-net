using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    public class CommunicatorException : Exception
    {
        public CommunicatorException(string message, Exception cause) : base(message, cause) { }
        public CommunicatorException(string message) : base(message) { }

        public class NotExistServerInfo : CommunicatorException
        {
            public NotExistServerInfo() : base("ServerInfo is not exist") { }
        }
    }
}
