using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Base
{
    public class BaseActor
    {
        public IActor Actor { get; }
        public XActorSender ActorSender { get; }

        public BaseActor(IActor actor, XActorSender actorSender)
        {
            Actor = actor;
            ActorSender = actorSender;
        }
    }
}
