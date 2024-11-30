namespace PlayHouse.Communicator.PlaySocket;

public class SocketConfig(int nid,string bindEndpoint, PlaySocketConfig playSocketConfig)
{
    public PlaySocketConfig PlaySocketConfig { get; set; } = playSocketConfig;
    public int Nid { get; internal set; } = nid;
    public string BindEndpoint { get; internal set; } = bindEndpoint;
}