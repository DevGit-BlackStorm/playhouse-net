using PlayHouse.Production;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;
class ServerAddressResolver
{
    private readonly string _bindEndpoint;
    private readonly XServerInfoCenter _serverInfoCenter;
    private readonly XClientCommunicator _communicateClient;
    private readonly IProcessor _service;
    private readonly IStorageClient _storageClient;
    private readonly LOG<ServerAddressResolver> _log = new ();

    private Timer? _timer;

    public ServerAddressResolver(string bindEndpoint, XServerInfoCenter serverInfoCenter,
        XClientCommunicator communicateClient, IProcessor service, IStorageClient storageClient)
    {
        this._bindEndpoint = bindEndpoint;
        this._serverInfoCenter = serverInfoCenter;
        this._communicateClient = communicateClient;
        this._service = service;
        this._storageClient = storageClient;
    }

    public void Start()
    {
        _log.Info(()=>"Server address resolver start");

        _timer = new Timer(_ =>
        {
            try
            {
                _storageClient.UpdateServerInfo(new XServerInfo(
                    _bindEndpoint,
                    _service.GetServiceType(),
                    _service.ServiceId,
                    _service.GetServerState(),
                    _service.GetWeightPoint(),
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));

                IList<XServerInfo> serverInfoList = _storageClient.GetServerList(_bindEndpoint);
                IList<XServerInfo> updateList = _serverInfoCenter.Update(serverInfoList);

                foreach (XServerInfo serverInfo in updateList)
                {
                    switch (serverInfo.State)
                    {
                        case ServerState.RUNNING:
                            _communicateClient.Connect(serverInfo.BindEndpoint);
                            break;
                        case ServerState.DISABLE:
                            _communicateClient.Disconnect(serverInfo.BindEndpoint);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(()=>e.Message);
            }
        }, null, ConstOption.AddressResolverInitialDelay, ConstOption.AddressResolverPeriod);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}
