using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Session
{
    public class SessionOption
    {
        public long ClientSessionIdleTimeout { get; set; }
        public List<string> Urls { get; set; }
        public int SessionPort { get; set; }
        public bool UseWebSocket { get; set; }

        public SessionOption()
        {
            ClientSessionIdleTimeout = 0;
            Urls = new List<string>();
            SessionPort = 0;
            UseWebSocket = false;
        }

    }
}
