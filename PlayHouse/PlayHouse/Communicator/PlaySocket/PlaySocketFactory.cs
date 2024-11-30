namespace PlayHouse.Communicator.PlaySocket;

internal abstract class PlaySocketFactory
{
    public static IPlaySocket CreatePlaySocket(SocketConfig config)
    {
        return new NetMqPlaySocket(config);
    }
}