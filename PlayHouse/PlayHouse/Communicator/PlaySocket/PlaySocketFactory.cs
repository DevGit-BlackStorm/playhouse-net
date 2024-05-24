namespace PlayHouse.Communicator.PlaySocket;

internal abstract class PlaySocketFactory
{
    public static IPlaySocket CreatePlaySocket(SocketConfig config, string bindEndpoint)
    {
        return new NetMqPlaySocket(config, bindEndpoint);
    }
}