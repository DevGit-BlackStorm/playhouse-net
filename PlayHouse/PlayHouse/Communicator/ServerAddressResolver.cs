using PlayHouse.Production.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal class ServerAddressResolver(
    string bindEndpoint,
    XServerInfoCenter serverInfoCenter,
    XClientCommunicator communicateClient,
    IService service,
    IServerInfoRetriever storageClient)
{
    private readonly LOG<ServerAddressResolver> _log = new();

    private Timer? _timer;

    public void Start()
    {
        _log.Info(() => "Server address resolver start");

        async void TimerCallback(object? _)
        {
            try
            {
                var myServerInfo = new XServerInfo(bindEndpoint, service.GetServiceType(), service.ServiceId, service.GetServerState(), service.GetActorCount(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                //자신의 정보먼저  update
                serverInfoCenter.Update(new List<XServerInfo> { myServerInfo });

                IList<XServerInfo> serverInfoList = await storageClient.UpdateServerListAsync(myServerInfo);

                var updateList = serverInfoCenter.Update(serverInfoList);

                foreach (var serverInfo in updateList)
                {
                    switch (serverInfo.GetState())
                    {
                        case ServerState.RUNNING:
                            communicateClient.Connect(serverInfo.GetBindEndpoint());
                            break;
                        case ServerState.DISABLE:
                            //_communicateClient.Disconnect(serverInfo.GetBindEndpoint());
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(() => e.Message);
            }
        }

        _timer = new Timer(TimerCallback, null, ConstOption.AddressResolverInitialDelayMs, ConstOption.AddressResolverPeriodMs);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}