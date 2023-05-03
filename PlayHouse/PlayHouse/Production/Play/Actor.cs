using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Service.Play;

namespace PlayHouse.Production.Play
{
    public interface IActor
    {
        IActorSender ActorSender { get; }
        Task OnCreate();
        Task OnDestroy();
    }
}
