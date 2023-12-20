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
        Task<(ushort errorCode, IPacket reply)> RequestToApi(IPacket packet);
        Task<(ushort errorCode, IPacket reply)> AsyncToApi(IPacket packet);
    }
}
