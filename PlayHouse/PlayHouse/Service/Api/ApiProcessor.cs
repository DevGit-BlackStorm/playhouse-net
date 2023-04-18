﻿using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Service.Session;
using System.Runtime.Caching;
using System.Threading;
using System.Collections.Specialized;
using Playhouse.Protocol;

namespace PlayHouse.Service.Api
{
    public class ApiProcessor : IProcessor
    {
        private readonly short _serviceId;
        private readonly ApiOption _apiOption;
        private readonly RequestCache _requestCache;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly ISender sender;
        private readonly XSystemPanel _systemPanel;

        private readonly AtomicEnumWrapper<ServerState> _state = new AtomicEnumWrapper<ServerState>(ServerState.DISABLE);
        private readonly ApiReflection _apiReflection;
        private readonly ConcurrentQueue<RoutePacket> _msgQueue = new ConcurrentQueue<RoutePacket>();

        private readonly MemoryCache _cache;
        private CacheItemPolicy _policy;
        private Thread _threadLoop;

        public ApiProcessor(
            short serviceId,
            ApiOption apiOption,
            RequestCache requestCache,
            IClientCommunicator clientCommunicator,
            ISender sender,
            XSystemPanel systemPanel)
        {
            _serviceId = serviceId;
            _apiOption = apiOption;
            _requestCache = requestCache;
            _clientCommunicator = clientCommunicator;
            this.sender = sender;
            _systemPanel = systemPanel;

            _apiReflection = new ApiReflection();

            _policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(60*5) };
            var cacheSettings = new NameValueCollection();
            cacheSettings.Add("CacheMemoryLimitMegabytes", "10");
            cacheSettings.Add("PhysicalMemoryLimitPercentage", "1");
            _cache = new MemoryCache("ApiProcessor", cacheSettings);

            _threadLoop = new Thread(() => MessageLoop()) { Name = "apiProcessor:message-loop"};

        }

        public void OnStart()
        {
            _state.Value = ServerState.RUNNING;
            _apiReflection.CallInitMethod(_systemPanel, sender);
            _threadLoop.Start();
        }

        private  void MessageLoop()
        {

            while (_state.Value != ServerState.DISABLE)
            {
                if (_msgQueue.TryDequeue(out var routePacket))
                {
                    var routeHeader = routePacket.RouteHeader;

                    try
                    {
                        if (routeHeader.AccountId != 0)
                        {
                            var accountApiProcessor =(AccountApiProcessor) _cache.Get($"{ routeHeader.AccountId}");
                            if (accountApiProcessor == null)
                            {
                                accountApiProcessor = new AccountApiProcessor(
                                    _serviceId,
                                    _requestCache,
                                    _clientCommunicator,
                                    _apiReflection,
                                    _apiOption.ApiCallBackHandler);

                                _cache.Add(new CacheItem(routeHeader.AccountId.ToString(), accountApiProcessor),_policy);
                            }
                            accountApiProcessor!.Dispatch(routePacket!);
                        }
                        else
                        {
                            var apiSender = new AllApiSender(_serviceId, _clientCommunicator, _requestCache);
                            apiSender.SetCurrentPacketHeader(routeHeader);
                            
                            ThreadPool.QueueUserWorkItem(state =>
                            {
                                try
                                {
                                    _apiReflection.CallMethod(
                                        routeHeader,
                                        routePacket.ToPacket(),
                                        routePacket.IsBackend(),
                                        apiSender);
                                }
                                catch (Exception e)
                                {
                                    apiSender.ErrorReply(routeHeader, (short)BaseErrorCode.SystemError);
                                    LOG.Error(e.StackTrace, this.GetType(), e);
                                }
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        LOG.Error(e.StackTrace, this.GetType(), e);
                    }
                }
                Thread.Sleep(10);

                //Task.Delay(10);
            }
        }

        public void OnReceive(RoutePacket routePacket)
        {
            _msgQueue.Enqueue(routePacket);
        }

        public void OnStop()
        {
            _state.Value = ServerState.DISABLE;
        }

        public int GetWeightPoint()
        {
            return 0;
        }

        public ServerState GetServerState()
        {
            return _state.Value;
        }

        public ServiceType GetServiceType()
        {
            return ServiceType.API;
        }

        public void Pause()
        {
            _state.Value = ServerState.PAUSE;
        }
        public void Resume()
        {
            _state.Value = ServerState.RUNNING;
        }

        public short GetServiceId()
        {
            return _serviceId;
        }
    }

}
