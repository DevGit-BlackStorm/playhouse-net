using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Production.Api;
using PlayHouse.Utils;
using System.Collections.Concurrent;

namespace PlayHouse.Service.Api
{
    public class AccountApiProcessor
    {
        private readonly ushort _serviceId;
        private readonly RequestCache _requestCache;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly ApiReflection _apiReflection;
        private readonly IApiCallBack _apiCallBack;

        private readonly ConcurrentQueue<RoutePacket> _msgQueue = new ConcurrentQueue<RoutePacket>();
        private readonly AtomicBoolean _isUsing = new AtomicBoolean(false);

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

        public async Task Dispatch(RoutePacket routePacket)
        {
            _msgQueue.Enqueue(routePacket);

            if (_isUsing.CompareAndSet(false, true))
            {
                while (_isUsing.Get())
                {
                    if (_msgQueue.TryDequeue(out var item))
                    {
                        
                        var routeHeader = item.RouteHeader;

                        if (routeHeader.IsBase)
                        {
                            if (routeHeader.MsgId == DisconnectNoticeMsg.Descriptor.Index)
                            {
                                //var disconnectNoticeMsg = DisconnectNoticeMsg.Parser.ParseFrom(item.Data);
                                _apiCallBack.OnDisconnect(routePacket.AccountId);
                            }
                            else
                            {
                                LOG.Error($"Invalid base Api packet: {routeHeader.MsgId}", this.GetType());
                            }
                        }
                        else
                        {
                            var apiSender = new AllApiSender(_serviceId, _clientCommunicator, _requestCache);
                            apiSender.SetCurrentPacketHeader(routeHeader);

                            try
                            {
                                
                                if (routeHeader.IsBase)
                                {
                                    await _apiReflection.BackendCallMethod(routeHeader, item.ToPacket(), apiSender)!;
                                }
                                else
                                {
                                    await _apiReflection.CallMethod(routeHeader, item.ToPacket(), apiSender)!;
                                }
                                
                                
                            }
                            catch (Exception e)
                            {
                                // Use this error code when it's set in the content.
                                // Use the default content error code if it's not set in the content.
                                if(routeHeader.Header.MsgSeq > 0)
                                {
                                    ushort errorCode = ExceptionContextStorage.ErrorCode;
                                    if (errorCode == (ushort)BaseErrorCode.Success)
                                    {
                                        errorCode = (ushort)BaseErrorCode.UncheckedContentsError;
                                    }

                                    apiSender.ErrorReply(routePacket.RouteHeader, errorCode);
                                }
                                
                                LOG.Error(e.StackTrace, this.GetType(), e);
                            }
                        }
                        
                    }
                    else
                    {
                        _isUsing.Set(false);
                    }
                }
            }
        }
    }

}