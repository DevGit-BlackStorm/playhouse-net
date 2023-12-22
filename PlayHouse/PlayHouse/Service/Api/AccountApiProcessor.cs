using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Production.Api;
using PlayHouse.Utils;
using System.Collections.Concurrent;

namespace PlayHouse.Service.Api
{
    internal class AccountApiProcessor
    {
        private readonly ushort _serviceId;
        private readonly RequestCache _requestCache;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly ApiReflection _apiReflection;
        private readonly IApiCallBack _apiCallBack;

        private readonly ConcurrentQueue<RoutePacket> _msgQueue = new();
        private readonly AtomicBoolean _isUsing = new(false);
        private readonly LOG<AccountApiProcessor> _log = new ();
        private Func<IPacket, IPacket>? packetProducer;

        public AccountApiProcessor(
            ushort serviceId,
            RequestCache requestCache,
            IClientCommunicator clientCommunicator,
            ApiReflection apiReflection,
            IApiCallBack apiCallBack)
        {
            _serviceId = serviceId;
            _requestCache = requestCache;
            _clientCommunicator = clientCommunicator;
            _apiReflection = apiReflection;
            _apiCallBack = apiCallBack;
        }

        public AccountApiProcessor(ushort serviceId, RequestCache requestCache, IClientCommunicator clientCommunicator, ApiReflection apiReflection, IApiCallBack apiCallBack, Func<IPacket, IPacket>? packetProducer) : this(serviceId, requestCache, clientCommunicator, apiReflection, apiCallBack)
        {
            this.packetProducer = packetProducer;
        }

        public async Task Dispatch(RoutePacket packet)
        {
            _msgQueue.Enqueue(packet);

            if (_isUsing.CompareAndSet(false, true))
            {
             
                while (_msgQueue.TryDequeue(out var routePacket))
                {
                    var routeHeader = routePacket.RouteHeader;
                    var apiSender = new AllApiSender(_serviceId, _clientCommunicator, _requestCache);
                    apiSender.SetCurrentPacketHeader(routeHeader);

                    AsyncStorage.AsyncCore.Init(apiSender);
                    ServiceAsyncContext.Init();

                    if (routeHeader.IsBase)
                    {
                        if (routeHeader.MsgId == DisconnectNoticeMsg.Descriptor.Index)
                        {
                            //var disconnectNoticeMsg = DisconnectNoticeMsg.Parser.ParseFrom(item.Data);
                            _apiCallBack.OnDisconnect(routePacket.AccountId);
                        }
                        else
                        {
                            _log.Error(()=>$"Invalid base Api packet - [packetInfo:{routeHeader}]");
                        }
                    }
                    else
                    {
                        try
                        {
                            // _log.Debug(()=>
                            //     $"Before Call Method - [packetInfo:{routeHeader}]"
                            // );

                            if (routeHeader.IsBackend)
                            {
                                await _apiReflection.BackendCallMethod(routeHeader, routePacket, apiSender);
                            }
                            else
                            {
                                await _apiReflection.CallMethod(routeHeader, routePacket, apiSender);
                            }
                        }
                        catch (ApiException.NotRegisterApiMethod e)
                        {
                            if (routeHeader.Header.MsgSeq > 0)
                            {
                                apiSender.ErrorReply(routePacket.RouteHeader, (ushort)BaseErrorCode.NotRegisteredMessage);    
                            }
                                
                            _log.Error(()=>e.Message);
                        }
                        catch (ApiException.NotRegisterApiInstance e)
                        {
                            if (routeHeader.Header.MsgSeq > 0)
                            {
                                apiSender.ErrorReply(routePacket.RouteHeader, (ushort)BaseErrorCode.SystemError);
                            }

                            _log.Error(()=>e.Message);
                        }
                        catch (Exception e)
                        {
                            if (routeHeader.Header.MsgSeq > 0)
                            {
                                apiSender.ErrorReply(packet.RouteHeader, (ushort)BaseErrorCode.UncheckedContentsError);
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
                                        
                    AsyncStorage.AsyncCore.Clear();
                    ServiceAsyncContext.Clear();
                }
                _isUsing.Set(false);
            }
        }
    }

}