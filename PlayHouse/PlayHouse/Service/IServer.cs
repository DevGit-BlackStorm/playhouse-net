using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    public interface IServer
    {
        void Start();
        void Stop();
        void AwaitTermination();
    }

}
