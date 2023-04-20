using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play
{
    public interface IActor
    {
        IActorSender ActorSender { get; }
        void OnCreate();
        void OnDestroy();
    }
}
