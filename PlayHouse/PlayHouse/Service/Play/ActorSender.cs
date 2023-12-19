using PlayHouse.Production;

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
        Task<ReplyPacket> RequestToApi(IPacket packet);
        Task<ReplyPacket> AsyncToApi(IPacket packet);
    }
}
