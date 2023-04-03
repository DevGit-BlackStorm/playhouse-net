using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    public interface IServerInfoCenter
    {
        IList<XServerInfo> Update(IList<XServerInfo> serverList);
        XServerInfo FindServer(string endpoint);
        XServerInfo FindRoundRobinServer(short serviceId);
        IList<XServerInfo> GetServerList();
    }
}
