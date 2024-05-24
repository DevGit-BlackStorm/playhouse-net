using PlayHouse.Service.Play;

namespace PlayHouse.Production.Play;

public interface IActor
{
    IActorSender ActorSender { get; }
    Task OnCreate();
    Task OnDestroy();
}