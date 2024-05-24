using System.Collections.Specialized;
using System.Runtime.Caching;
using Microsoft.Extensions.DependencyInjection;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Api;
using Playhouse.Protocol;
using PlayHouse.Service.Api.Reflection;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Service.Api;

internal class ApiDispatcher
{
    private readonly ApiReflection _apiReflection;
    private readonly ApiReflectionCallback _apiReflectionCallback;
    private readonly MemoryCache _cache;
    private readonly IClientCommunicator _clientCommunicator;
    private readonly LOG<ApiService> _log = new();
    private readonly CacheItemPolicy _policy;
    private readonly RequestCache _requestCache;
    private readonly ushort _serviceId;

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


        var controllerTester = serviceProvider.GetService<ControllerTester>();
        controllerTester?.Init(_apiReflection);


        _apiReflectionCallback = new ApiReflectionCallback(serviceProvider);

        _policy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(5) };
        var cacheSettings = new NameValueCollection
        {
            { "CacheMemoryLimitMegabytes", "10" },
            { "PhysicalMemoryLimitPercentage", "1" }
        };
        _cache = new MemoryCache("ApiService", cacheSettings);
    }

    public void Start()
    {
    }

    public void Stop()
    {
    }


    internal int GetAccountCount()
    {
        return _cache.Count();
    }

    internal void OnPost(RoutePacket routePacket)
    {
        using (routePacket)
        {
            var routeHeader = routePacket.RouteHeader;

            if (routeHeader.AccountId != 0)
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

                apiActor.Post(RoutePacket.MoveOf(routePacket));
            }
            else
            {
                Task.Run(async () => { await DispatchAsync(RoutePacket.MoveOf(routePacket)); });
            }
        }
    }

    private async Task DispatchAsync(RoutePacket routePacket)
    {
        var routeHeader = routePacket.RouteHeader;
        var apiSender = new AllApiSender(_serviceId, _clientCommunicator, _requestCache);
        apiSender.SetCurrentPacketHeader(routeHeader);

        if (routeHeader.IsBase && routeHeader.MsgId == UpdateServerInfoReq.Descriptor.Index)
        {
            var updateServerInfoReq = UpdateServerInfoReq.Parser.ParseFrom(routePacket.Span);

            _clientCommunicator.Connect(updateServerInfoReq.ServerInfo.Endpoint);
            var serverInfoList =
                await _apiReflectionCallback.UpdateServerInfoAsync(XServerInfo.Of(updateServerInfoReq.ServerInfo));
            UpdateServerInfoRes updateServerInfoRes = new();
            updateServerInfoRes.ServerInfos.AddRange(serverInfoList.Select(e => XServerInfo.Of(e).ToMsg()));
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