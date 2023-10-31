namespace PlayHouse.Communicator;
public interface IStorageClient
{
    void UpdateServerInfo(XServerInfo serverInfo);
    List<XServerInfo> GetServerList(string endpoint);
}
