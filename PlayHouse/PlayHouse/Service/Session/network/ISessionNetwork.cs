using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Session.network
{
    internal interface ISessionNetwork
    {
        void Start();
        void Stop();
        void Restart();
    }
}
