using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    public interface IStorageClient
    {
        void UpdateServerInfo(XServerInfo serverInfo);
        List<XServerInfo> GetServerList(string endpoint);
    }
}
