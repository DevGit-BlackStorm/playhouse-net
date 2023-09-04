using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Communicator;

namespace PlayHouse.Production
{
    public interface IServerInfoCenter
    {
        IList<XServerInfo> Update(IList<XServerInfo> serverList);
        XServerInfo FindServer(string endpoint);
        XServerInfo FindRoundRobinServer(short serviceId);
        IList<XServerInfo> GetServerList();
        XServerInfo FindServerByAccountId(short serviceId, Guid accountId);
        ServiceType FindServerType(short serviceId);
    }
}
