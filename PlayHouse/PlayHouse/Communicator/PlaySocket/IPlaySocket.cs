using PlayHouse.Communicator.Message;

namespace PlayHouse.Communicator.PlaySocket
{
    internal interface IPlaySocket
    {
        String GetBindEndpoint();
        void Bind();
        void Send(String endpoint, RoutePacket routerPacket);
        void Connect(String target);
        RoutePacket? Receive();
        void Disconnect(String endpoint);

        void Close();

        String Id();

    }
}
