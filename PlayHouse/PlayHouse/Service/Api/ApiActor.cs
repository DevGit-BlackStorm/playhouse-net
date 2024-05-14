using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Service.Api.Reflection;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;
using System.Collections.Concurrent;

namespace PlayHouse.Service.Api
{
    internal class ApiActor
    {
        private readonly LOG<ApiActor> _log = new();
        private readonly ushort _serviceId;
        private readonly RequestCache _requestCache;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly ApiReflection _apiReflection;
        private readonly ApiReflectionCallback _apiReflectionCallback;

        private readonly ConcurrentQueue<RoutePacket> _msgQueue = new();
        private readonly AtomicBoolean _isUsing = new(false);
        
        public ApiActor(
            ushort serviceId,
            RequestCache requestCache,
            IClientCommunicator clientCommunicator,
            ApiReflection apiReflection,
            ApiReflectionCallback apiReflectionCallback
            )
        {
            _serviceId = serviceId;
            _requestCache = requestCache;
            _clientCommunicator = clientCommunicator;
            _apiReflection = apiReflection;
            _apiReflectionCallback = apiReflectionCallback;
        }

        public async Task DispatchAsync(RoutePacket routePacket)
        {
            var routeHeader = routePacket.RouteHeader;
            var apiSender = new AllApiSender(_serviceId, _clientCommunicator, _requestCache);
            apiSender.SetCurrentPacketHeader(routeHeader);

            try
            {
                if (routeHeader.IsBase)
                {
                    if (routeHeader.MsgId == DisconnectNoticeMsg.Descriptor.Index)
                    {
                        try
                        {

                            await _apiReflectionCallback.OnDisconnectAsync(apiSender);
                        }
                        catch (Exception e)
                        {
                            _log.Error(() => "exception message:" + e.Message);
                            _log.Error(() => "exception trace:" + e.StackTrace);

                            if (e.InnerException != null)
                            {
                                _log.Error(() => "internal exception message:" + e.InnerException.Message);
                                _log.Error(() => "internal exception trace:" + e.InnerException.StackTrace);
                            }
                        }

                    }
                    else
                    {
                        _log.Error(() => $"Invalid base Api packet - [packetInfo:{routeHeader}]");
                    }
                }
                else
                {

                    if (routeHeader.IsBackend)
                    {
                        await _apiReflection.CallBackendMethodAsync(routePacket.ToContentsPacket(), apiSender);
                    }
                    else
                    {
                        await _apiReflection.CallMethodAsync(routePacket.ToContentsPacket(), apiSender);
                    }
                }
            }
            catch (ServiceException.NotRegisterMethod e)
            {
                if (routeHeader.Header.MsgSeq > 0)
                {
                    apiSender.Reply((ushort)BaseErrorCode.NotRegisteredMessage);
                }

                _log.Error(() => e.Message);
            }
            catch (ServiceException.NotRegisterInstance e)
            {
                if (routeHeader.Header.MsgSeq > 0)
                {
                    apiSender.Reply((ushort)BaseErrorCode.SystemError);
                }

                _log.Error(() => e.Message);
            }
            catch (Exception e)
            {
                if (routeHeader.Header.MsgSeq > 0)
                {
                    apiSender.Reply((ushort)BaseErrorCode.UncheckedContentsError);
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
        }

        public void Post(RoutePacket packet)
        {
            _msgQueue.Enqueue(packet);

            if (_isUsing.CompareAndSet(false, true))
            {
                Task.Run(async () =>
                {
                    while (_msgQueue.TryDequeue(out var routePacket))
                    {
                        using (routePacket)
                        {
                            await DispatchAsync(routePacket);
                        }
                    }
                    _isUsing.Set(false);
                });
             
            }
        }
    }

}