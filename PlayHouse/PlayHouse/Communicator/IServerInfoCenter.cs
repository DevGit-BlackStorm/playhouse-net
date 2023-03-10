using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    interface IServerInfoCenter
    {
        IList<XServerInfo> Update(IList<XServerInfo> serverList);
        XServerInfo FindServer(string endpoint);
        XServerInfo FindRoundRobinServer(string serviceId);
        IList<XServerInfo> GetServerList();
    }
}
