namespace PlayHouse.Communicator;
internal interface IStorageClient
{
    void UpdateServerInfo(XServerInfo serverInfo);
    List<XServerInfo> GetServerList(string endpoint);
}
