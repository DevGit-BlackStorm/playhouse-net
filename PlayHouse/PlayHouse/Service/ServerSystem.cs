using PlayHouse.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    public interface IServerSystem
    {
        
        Task OnStart();
        Task OnDispatch(Packet packet);
        Task OnStop();
        Task OnPause();
        Task OnResume();
    }
}
