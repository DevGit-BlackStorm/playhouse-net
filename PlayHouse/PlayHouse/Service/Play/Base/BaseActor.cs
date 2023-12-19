using PlayHouse.Production.Play;

namespace PlayHouse.Service.Play.Base;
internal class BaseActor
{
    public IActor Actor { get; }
    public XActorSender ActorSender { get; }

    public BaseActor(IActor actor, XActorSender actorSender)
    {
        Actor = actor;
        ActorSender = actorSender;
    }
}
