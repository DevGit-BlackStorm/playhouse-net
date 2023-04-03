using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
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
        short ServiceId();
        ServerState State();
        //int WeightingPoint();
        long TimeStamp();
    }
}
