using PlayHouse.Production.Shared;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;
using System.Security.Cryptography.Xml;

namespace PlayHouse.Communicator;
class ServerAddressResolver
{
    private readonly string _bindEndpoint;
    private readonly XServerInfoCenter _serverInfoCenter;
    private readonly XClientCommunicator _communicateClient;
    private readonly IService _service;
    private readonly IServerInfoRetriever _serverRetriever;
    private readonly LOG<ServerAddressResolver> _log = new ();

    private Timer? _timer;

    public ServerAddressResolver(string bindEndpoint, XServerInfoCenter serverInfoCenter,
        XClientCommunicator communicateClient, IService service, IServerInfoRetriever storageClient)
    {
        this._bindEndpoint = bindEndpoint;
        this._serverInfoCenter = serverInfoCenter;
        this._communicateClient = communicateClient;
        this._service = service;
        this._serverRetriever = storageClient;
    }

    public void Start()
    {
        _log.Info(()=>"Server address resolver start");

        _timer = new Timer( async _ =>
        {
            

            try
            {
                XServerInfo myServerInfo = new XServerInfo(
                                    _bindEndpoint,
                                    _service.GetServiceType(),
                                    _service.ServiceId,
                                    _service.GetServerState(),
                                    _service.GetActorCount(),
                                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                );

                //자신의 정보먼저  update
                _serverInfoCenter.Update(new List<XServerInfo>() { myServerInfo });

                IList<XServerInfo> serverInfoList = await _serverRetriever.UpdateServerListAsync(myServerInfo);

                IList<XServerInfo> updateList = _serverInfoCenter.Update(serverInfoList);

                foreach (XServerInfo serverInfo in updateList)
                {
                    switch (serverInfo.GetState())
                    {
                        case ServerState.RUNNING:
                            _communicateClient.Connect(serverInfo.GetBindEndpoint());
                            break;
                        case ServerState.DISABLE:
                            //_communicateClient.Disconnect(serverInfo.GetBindEndpoint());
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(()=> e.Message);
            }finally
            {
                ///ServiceAsyncContext.Clear();
            }

        }, null, ConstOption.AddressResolverInitialDelayMs, ConstOption.AddressResolverPeriodMs);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}
