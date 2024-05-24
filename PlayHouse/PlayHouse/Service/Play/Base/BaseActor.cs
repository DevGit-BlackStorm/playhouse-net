using PlayHouse.Production.Play;

namespace PlayHouse.Service.Play.Base;

internal class BaseActor(IActor actor, XActorSender actorSender)
{
    public IActor Actor { get; } = actor;
    public XActorSender ActorSender { get; } = actorSender;
}