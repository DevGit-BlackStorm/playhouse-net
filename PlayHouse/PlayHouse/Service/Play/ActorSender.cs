using PlayHouse.Production;

namespace PlayHouse.Service.Play
{
    public interface IActorSender
    {
        Guid AccountId();
        string SessionEndpoint();
        string ApiEndpoint();
        int Sid();
        void LeaveStage();

        void SendToClient(Packet packet);

        void SendToApi(Packet packet);
        Task<ReplyPacket> RequestToApi(Packet packet);
        Task<ReplyPacket> AsyncToApi(Packet packet);
    }
}
