using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Collections.Specialized;
using Playhouse.Protocol;
using PlayHouse.Utils;
using PlayHouse.Production;
using PlayHouse.Production.Api;

namespace PlayHouse.Service.Api
{
    internal class ApiProcessor : IProcessor
    {
        private readonly LOG<ApiProcessor> _log = new ();
        private readonly ushort _serviceId;
        private readonly ApiOption _apiOption;
        private readonly RequestCache _requestCache;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly ISender _sender;
        private readonly XSystemPanel _systemPanel;
        private readonly AtomicEnum<ServerState> _state = new(ServerState.DISABLE);
        private readonly ApiReflection _apiReflection;
        private readonly ConcurrentQueue<RoutePacket> _msgQueue = new();

        private readonly MemoryCache _cache;
        private readonly CacheItemPolicy _policy;
        private readonly Thread _threadLoop;

        public ushort ServiceId => _serviceId;

        public ApiProcessor(
            ushort serviceId,
            ApiOption apiOption,
            RequestCache requestCache,
            IClientCommunicator clientCommunicator,
            ISender sender,
            XSystemPanel systemPanel
        )
        {
            _serviceId = serviceId;
            _apiOption = apiOption;
            _requestCache = requestCache;
            _clientCommunicator = clientCommunicator;
            _sender = sender;
            _systemPanel = systemPanel;
            _apiReflection = new ApiReflection();

            _policy = new CacheItemPolicy { SlidingExpiration =TimeSpan.FromMinutes(5) };
            var cacheSettings = new NameValueCollection
            {
                { "CacheMemoryLimitMegabytes", "10" },
                { "PhysicalMemoryLimitPercentage", "1" }
            };
            _cache = new MemoryCache("ApiProcessor", cacheSettings);

            _threadLoop = new Thread(MessageLoop) { Name = "apiProcessor:message-loop"};

        }

        public void OnStart()
        {
            _state.Set(ServerState.RUNNING);
            _threadLoop.Start();
        }

        private  void MessageLoop()
        {

            while (_state.Get() != ServerState.DISABLE)
            {
                while (_msgQueue.TryDequeue(out var routePacket))
                {
                    var routeHeader = routePacket.RouteHeader;

                    try
                    {
                        if (routeHeader.AccountId != string.Empty)
                        {
                            var accountApiProcessor =(AccountApiProcessor?) _cache.Get($"{ routeHeader.AccountId}");
                            if (accountApiProcessor == null)
                            {
                                accountApiProcessor = new AccountApiProcessor
                                (
                                    _serviceId,
                                    _requestCache,
                                    _clientCommunicator,
                                    _apiReflection,
                                    _apiOption.ApiCallBackHandler!

                                );

                                _cache.Add(new CacheItem(routeHeader.AccountId.ToString(), accountApiProcessor),_policy);
                            }

                            var result = routePacket;
                            Task.Run( async ()  => {
                                 await accountApiProcessor.Dispatch(result).ConfigureAwait(false);
                             });
                            
                        }
                        else
                        {
                            var packet = routePacket;
                            _ = Task.Run(async () =>
                            {
                                var apiSender = new AllApiSender(_serviceId, _clientCommunicator, _requestCache);
                                apiSender.SetCurrentPacketHeader(routeHeader);

                                AsyncContext.Init();
                                ApiAsyncContext.Init(apiSender);
                                SenderAsyncContext.Init();

                                try
                                {
                                    if(packet.IsBackend())
                                    {
                                        await _apiReflection.BackendCallMethod(
                                        routeHeader,
                                        packet.ToPacket(),
                                        apiSender).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await _apiReflection.CallMethod(
                                        routeHeader,
                                        packet.ToPacket(),
                                        apiSender).ConfigureAwait(false);
                                    }
                                }
                                catch (ApiException.NotRegisterApiMethod e)
                                {
                                    if (routeHeader.Header.MsgSeq > 0)
                                    {
                                        apiSender.ErrorReply(packet.RouteHeader, (ushort)BaseErrorCode.NotRegisteredMessage);    
                                    }
                                    _log.Error(() => e.Message);
                                }
                                catch (ApiException.NotRegisterApiInstance e)
                                {
                                    if (routeHeader.Header.MsgSeq > 0)
                                    {
                                        apiSender.ErrorReply(packet.RouteHeader, (ushort)BaseErrorCode.SystemError);    
                                    }
                                    _log.Error(() => e.Message);
                                }
                                catch (Exception e)
                                {
                                    // Use this error code when it's set in the content.
                                    // Use the default content error code if it's not set in the content.
                                    if(routeHeader.Header.MsgSeq > 0)
                                    {
                                        var errorCode = ApiAsyncContext.ErrorCode;
                                        if (errorCode == (ushort)BaseErrorCode.Success)
                                        {
                                            errorCode = (ushort)BaseErrorCode.UncheckedContentsError;
                                        }

                                        apiSender.ErrorReply(packet.RouteHeader, errorCode);
                                    }

                                    _log.Error(() => $"Packet processing failed due to an unexpected error. - [msgId:{routeHeader.MsgId}]");
                                    _log.Error(() => "exception message:" + e.Message);
                                    _log.Error(() => "exception trace:" + e.StackTrace);

                                    if (e.InnerException != null)
                                    {
                                        _log.Error(() => "internal exception message:" + e.InnerException.Message);
                                        _log.Error(() => "internal exception trace:" + e.InnerException.StackTrace);
                                    }
                                }

                                AsyncContext.Clear();
                                ApiAsyncContext.Clear();
                                SenderAsyncContext.Clear();
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error(() => $"Packet processing failed due to an unexpected error. - [msgId:{routeHeader.MsgId}]");
                        _log.Error(() => "exception message:" + e.Message);
                        _log.Error(() => "exception trace:" + e.StackTrace);

                        if (e.InnerException != null)
                        {
                            _log.Error(() => "internal exception message:" + e.InnerException.Message);
                            _log.Error(() => "internal exception trace:" + e.InnerException.StackTrace);
                        }
                    }
                }
                Thread.Sleep(ConstOption.ThreadSleep);
            }
        }

        public void OnReceive(RoutePacket routePacket)
        {
            _msgQueue.Enqueue(routePacket);
        }

        public void OnStop()
        {
            _state.Set( ServerState.DISABLE);
        }

        public int GetWeightPoint()
        {
            return 0;
        }

        public ServerState GetServerState()
        {
            return _state.Get();
        }

        public ServiceType GetServiceType()
        {
            return ServiceType.API;
        }

        public void Pause()
        {
            _state.Set(ServerState.PAUSE);
        }
        public void Resume()
        {
            _state.Set(ServerState.RUNNING);
        }

        public ushort GetServiceId()
        {
            return _serviceId;
        }
    }

}
