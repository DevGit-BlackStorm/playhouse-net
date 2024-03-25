using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play
{
    public interface IActorSender
    {
        string AccountId();
        string SessionEndpoint();
        string ApiEndpoint();
        int Sid();
        void LeaveStage();

        void SendToClient(IPacket packet);

        void SendToApi(IPacket packet);
        Task<IPacket> RequestToApi(IPacket packet);
        Task<IPacket> AsyncToApi(IPacket packet);
    }
}
