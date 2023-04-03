using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    public interface IServerSystem
    {
        ISystemPanel SystemPanel { get; }
        ICommonSender BaseSender { get; }
        Task OnStart();
        Task OnDispatch(Packet packet);
        Task OnStop();
        Task OnPause();
        Task OnResume();
    }
}
