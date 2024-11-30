using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play;

public interface IActorSender
{
    long AccountId();
    int SessionNid();
    int ApiNid();
    long Sid();
    void LeaveStage();

    void SendToClient(IPacket packet);

    void SendToApi(IPacket packet);
    Task<IPacket> RequestToApi(IPacket packet);
    Task<IPacket> AsyncToApi(IPacket packet);
}