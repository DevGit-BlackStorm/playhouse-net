using Microsoft.Extensions.DependencyInjection;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Api;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Api.Reflection;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;
using System.Collections.Specialized;
using System.Runtime.Caching;

namespace PlayHouse.Service.Api;

internal class ApiDispatcher
{
    private readonly LOG<ApiService> _log = new();
    private readonly ushort _serviceId;
    private readonly RequestCache _requestCache;
    private readonly IClientCommunicator _clientCommunicator;
    private readonly ApiReflection _apiReflection;
    private readonly ApiReflectionCallback _apiReflectionCallback;
    private readonly CacheItemPolicy _policy;
    private readonly MemoryCache _cache;
    private readonly PacketWorkerQueue _workerQueue;

    public ApiDispatcher(
        ushort serviceId,
        RequestCache requestCache,
        IClientCommunicator clientCommunicator,
        IServiceProvider serviceProvider,
        ApiOption apiOption
        )
    {
        _serviceId = serviceId;
        _requestCache = requestCache;
        _clientCommunicator = clientCommunicator;
        _apiReflection = new ApiReflection(serviceProvider, apiOption.AspectifyManager);


        ControllerTester? controllerTester = serviceProvider.GetService<ControllerTester>();
        if(controllerTester != null ) 
        { 
            controllerTester.Init(_apiReflection);
        }
        

        //ControllerTester.Init(_apiReflection);

        _apiReflectionCallback = new ApiReflectionCallback(serviceProvider);

        _policy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(5) };
        var cacheSettings = new NameValueCollection
            {
                { "CacheMemoryLimitMegabytes", "10" },
                { "PhysicalMemoryLimitPercentage", "1" }
            };
        _cache = new MemoryCache("ApiService", cacheSettings);
        _workerQueue = new PacketWorkerQueue(DispatchAsync);
    }

    public void Start()
    {
        _workerQueue.Start();
    }
    public void Stop() 
    { 
        _workerQueue.Stop();
    }

    public async Task DispatchAsync(RoutePacket routePacket)
    {
        var routeHeader = routePacket.RouteHeader;

        using (routePacket)
        {

            if (routeHeader.AccountId != string.Empty)
            {
                var apiActor = (ApiActor?)_cache.Get($"{routeHeader.AccountId}");
                if (apiActor == null)
                {
                    apiActor = new ApiActor
                    (
                        _serviceId,
                        _requestCache,
                        _clientCommunicator,
                        _apiReflection,
                        _apiReflectionCallback
                    );

                    _cache.Add(new CacheItem(routeHeader.AccountId.ToString(), apiActor), _policy);
                }

                await apiActor.PostAsync(RoutePacket.MoveOf(routePacket));
            }
            else
            {
                var apiSender = new AllApiSender(_serviceId, _clientCommunicator, _requestCache);
                apiSender.SetCurrentPacketHeader(routeHeader);

                if (routeHeader.IsBase && routeHeader.MsgId == UpdateServerInfoReq.Descriptor.Index)
                {

                    var updateServerInfoReq = UpdateServerInfoReq.Parser.ParseFrom(routePacket.Span);

                    _clientCommunicator.Connect(updateServerInfoReq.ServerInfo.Endpoint);
                    List<IServerInfo> serverInfoList = await _apiReflectionCallback.UpdateServerInfoAsync(XServerInfo.Of(updateServerInfoReq.ServerInfo));
                    UpdateServerInfoRes updateServerInfoRes = new();
                    updateServerInfoRes.ServerInfos.AddRange(serverInfoList.Select(e=>XServerInfo.Of(e).ToMsg()));
                    apiSender.Reply(XPacket.Of(updateServerInfoRes));

                    return;
                }

                if (routePacket.IsBackend())
                {
                    await _apiReflection.CallBackendMethodAsync(routePacket.ToContentsPacket(), apiSender);
                }
                else
                {
                    await _apiReflection.CallMethodAsync(routePacket.ToContentsPacket(), apiSender);
                }
            }
        }
    }


    internal int GetAccountCount()
    {
        return _cache.Count();
    }

    internal void OnPost(RoutePacket routePacket)
    {
        _workerQueue.Post(routePacket);
    }
}
