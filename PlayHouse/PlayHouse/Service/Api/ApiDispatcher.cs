﻿using Microsoft.Extensions.DependencyInjection;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Api;
using PlayHouse.Service.Api.Reflection;
using PlayHouse.Utils;
using System.Collections.Concurrent;
using Playhouse.Protocol;

namespace PlayHouse.Service.Api;

internal class ApiDispatcher
{
    private readonly ApiReflection _apiReflection;
    private readonly ApiReflectionCallback _apiReflectionCallback;
    private readonly IClientCommunicator _clientCommunicator;
    private readonly LOG<ApiService> _log = new();
    private readonly RequestCache _requestCache;
    private readonly ushort _serviceId;
    private readonly ConcurrentDictionary<long, ApiActor> _cache = new();

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
        _apiReflectionCallback = new ApiReflectionCallback(serviceProvider);


        var controllerTester = serviceProvider.GetService<ControllerTester>();
        controllerTester?.Init(_apiReflection, _apiReflectionCallback);

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
            var accountId = routePacket.AccountId;
            if (accountId != 0)
            {
                var apiActor = Get(accountId);
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

                    _cache[accountId] = apiActor;
                }

                apiActor.Post(RoutePacket.MoveOf(routePacket));

                if (routePacket.MsgId == DisconnectNoticeMsg.Descriptor.Name)
                {
                    Remove(accountId);
                }
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

        if (routePacket.IsBackend())
        {
            await _apiReflection.CallBackendMethodAsync(routePacket.ToContentsPacket(), apiSender);
        }
        else
        {
            await _apiReflection.CallMethodAsync(routePacket.ToContentsPacket(), apiSender);
        }
    }
    private void Remove(long accountId)
    {
        _cache.TryRemove(accountId, out var _);
    }

    public ApiActor? Get(long accountId)
    {
        return _cache.GetValueOrDefault(accountId);
    }

}