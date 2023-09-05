using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Communicator;

namespace PlayHouse.Production
{

    public enum ServerState
    {
        RUNNING,
        PAUSE,
        DISABLE
    }


    public interface IServerInfo
    {
        string BindEndpoint();
        ServiceType ServiceType();
        ushort ServiceId();
        ServerState State();
        //int WeightingPoint();
        long TimeStamp();
    }
}
