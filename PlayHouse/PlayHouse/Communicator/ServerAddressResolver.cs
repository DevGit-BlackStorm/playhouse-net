using PlayHouse.Production.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal class ServerAddressResolver(
    string bindEndpoint,
    XServerInfoCenter serverInfoCenter,
    XClientCommunicator communicateClient,
    IService service,
    ISystemController system)
{
    private readonly LOG<ServerAddressResolver> _log = new();

    private Timer? _timer;


    public void Start()
    {
        _log.Info(() => $"Server address resolver start");

        async void TimerCallback(object? _)
        {
            try
            {
                var myServerInfo = new XServerInfo(bindEndpoint, service.ServiceId,service.ServerId, service.Nid, service.GetServiceType(),
                    service.GetServerState(), service.GetActorCount(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                IReadOnlyList<IServerInfo> serverInfoList = await system.UpdateServerInfoAsync(myServerInfo);

                var updateList = serverInfoCenter.Update(serverInfoList.Select(e =>
                    new XServerInfo(e.GetBindEndpoint(), e.GetServiceId(), e.GetServerId(),e.GetNid(),e.GetServiceType(),  e.GetState(), e.GetActorCount(), e.GetLastUpdate())
                ).ToList());

                foreach (var serverInfo in updateList)
                {
                    switch (serverInfo.GetState())
                    {
                        case ServerState.RUNNING:
                            communicateClient.Connect(serverInfo.GetBindEndpoint());
                            break;
                        case ServerState.DISABLE:
                            communicateClient.Disconnect(serverInfo.GetBindEndpoint());
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                _log.Error(() => $"{e}");
            }
        }

        _timer = new Timer(TimerCallback, null, ConstOption.AddressResolverInitialDelayMs,
            ConstOption.AddressResolverPeriodMs);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}