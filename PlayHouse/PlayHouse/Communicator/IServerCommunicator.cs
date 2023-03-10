using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    public interface IServerCommunicator 
    { 
        void Bind(ICommunicateListener listener);
        void Communicate();
        void Stop();
    }
}
