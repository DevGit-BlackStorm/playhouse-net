using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Utils;
using PlayHouse.Communicator;
using System.Collections.Concurrent;
using static NetMQ.NetMQSelector;
using PlayHouse.Service.Api;
using PlayHouse.Service.Play.Base.Command;

namespace PlayHouse.Service.Play.Base
{
    public class BaseStage
    {
        private long _stageId;
        private PlayProcessor _playService;
        private IClientCommunicator _clientCommunicator;
        private RequestCache _reqCache;
        private IServerInfoCenter _serverInfoCenter;
        private XStageSender _stageSender;
        private BaseStageCmdHandler _msgHandler = new BaseStageCmdHandler();
        private ConcurrentQueue<RoutePacket> _msgQueue = new ConcurrentQueue<RoutePacket>();
        private AtomicBoolean _isUsing = new AtomicBoolean(false);

        public XStageSender StageSender => _stageSender;

        private IStage<IActor>? _stage; 
        public bool IsCreated { get; private set; }

        public BaseStage(long stageId, PlayProcessor playService, IClientCommunicator clientCommunicator,
                         RequestCache reqCache, IServerInfoCenter serverInfoCenter,
                         XStageSender? stageSender = null)
        {
            _stageId = stageId;
            _playService = playService;
            _clientCommunicator = clientCommunicator;
            _reqCache = reqCache;
            _serverInfoCenter = serverInfoCenter;
            _stageSender = stageSender ?? new XStageSender(playService.ServiceId, stageId, playService, clientCommunicator, reqCache);


            _msgHandler.Register(CreateStageReq.Descriptor.Index, new CreateStageCmd(playService));
            _msgHandler.Register(JoinStageReq.Descriptor.Index, new JoinStageCmd(playService));
            _msgHandler.Register(CreateJoinStageReq.Descriptor.Index, new CreateJoinStageCmd(playService));
            _msgHandler.Register(StageTimer.Descriptor.Index, new StageTimerCmd(playService));
            _msgHandler.Register(DisconnectNoticeMsg.Descriptor.Index, new DisconnectNoticeCmd(playService));
            _msgHandler.Register(AsyncBlock.Descriptor.Index, new AsyncBlockCmd(playService));
        }

        private async Task Dispatch(RoutePacket routePacket)
        {
            _stageSender.SetCurrentPacketHeader(routePacket.RouteHeader);
            try
            {
                if (routePacket.IsBase())
                {
                    await _msgHandler.Dispatch(this, routePacket);
                }
                else
                {
                    long accountId = routePacket.AccountId;
                    var baseUser = _playService.FindUser(accountId);
                    if (baseUser != null)
                    {
                        await _stage!.OnDispatch(baseUser.Actor, new Packet(routePacket.GetMsgId(), routePacket.MovePayload()));
                    }
                }
            }
            catch (Exception e)
            {
                _stageSender.ErrorReply(routePacket.RouteHeader,(short) BaseErrorCode.SystemError);
                LOG.Error(e.StackTrace, this.GetType(), e);
            }
            finally
            {
                _stageSender.ClearCurrentPacketHeader();
            }

        }

        public async Task Send(RoutePacket routePacket)
        {
            _msgQueue.Enqueue(routePacket);
            if (_isUsing.CompareAndSet(false, true))
            {
                while (_isUsing.Get())
                {
                    if (_msgQueue.TryDequeue(out var item))
                    {
                        try
                        {
                            using (item)
                            {
                                await Dispatch(item);
                            }
                        }
                        catch (Exception e)
                        {
                            _stageSender.ErrorReply(routePacket.RouteHeader, (short)BaseErrorCode.UncheckedContentsError);
                            LOG.Error(e.StackTrace, this.GetType(), e);
                        }
                    }
                    else
                    {
                        _isUsing.Set(false);
                    }
                }
            }
        }

        public async Task<ReplyPacket> Create(string stageType, Packet packet)
        {
            _stage = _playService.CreateContentRoom(stageType, _stageSender);
            _stageSender.SetStageType(stageType);
            var outcome = await _stage.OnCreate(packet);
            IsCreated = true;
            return outcome;
        }

        public async Task<(ReplyPacket, int)> Join(long accountId, string sessionEndpoint, int sid, string apiEndpoint, Packet packet)
        {
            BaseActor? baseUser = _playService.FindUser(accountId);

            if (baseUser == null)
            {
                XActorSender userSender = new XActorSender(accountId, sessionEndpoint, sid, apiEndpoint, this, _serverInfoCenter);
                IActor user = _playService.CreateContentUser(_stageSender.StageType, userSender);
                baseUser = new BaseActor(user, userSender);
                baseUser.Actor.OnCreate();
                _playService.AddUser(baseUser);
            }
            else
            {
                baseUser.ActorSender.Update(sessionEndpoint, sid, apiEndpoint);
            }

            var outcome = await _stage!.OnJoinStage(baseUser.Actor, packet);
            int stageIndex = 0;

            if (!outcome.IsSuccess())
            {
                _playService.RemoveUser(accountId);
            }
            else
            {
                stageIndex = await UpdateSessionRoomInfo(sessionEndpoint, sid);
            }

            return new (outcome, stageIndex);
        }

        private async Task<int> UpdateSessionRoomInfo(string sessionEndpoint, int sid)
        {
            var joinStageInfoUpdateReq = new JoinStageInfoUpdateReq()
            {
                StageId = this.StageId,
                PlayEndpoint = _playService.Endpoint(),
            };

            var res = await _stageSender.RequestToBaseSession(sessionEndpoint, sid, new Packet(joinStageInfoUpdateReq));
            var result = JoinStageInfoUpdateRes.Parser.ParseFrom(res.Data);
            return result.StageIdx;
        }

        public void Reply(ReplyPacket packet)
        {
            this._stageSender.Reply(packet);
        }

        public void LeaveStage(long accountId, string sessionEndpoint, int sid)
        {
            this._playService.RemoveUser(accountId);
            var request = new LeaveStageMsg();
            this._stageSender.SendToBaseSession(sessionEndpoint, sid,new Packet(request));
        }

        public long StageId=> _stageSender.StageId;

        public void CancelTimer(long timerId)
        {
            this._stageSender.CancelTimer(timerId);
        }

        public bool HasTimer(long timerId)
        {
            return this._stageSender.HasTimer(timerId);
        }

        public async Task OnPostCreate()
        {
            try
            {
                await _stage!.OnPostCreate();
            }
            catch (Exception e)
            {
                LOG.Error(e.StackTrace, this.GetType(), e);
            }
        }

        public async Task OnPostJoinRoom(long accountId)
        {
            try
            {
                BaseActor? baseUser = _playService.FindUser(accountId);

                if (baseUser != null)
                {
                    await _stage!.OnPostJoinStage(baseUser.Actor);
                }
                else
                {
                    LOG.Error($"user is not exist : {accountId}", this.GetType());
                }

            }
            catch (Exception e)
            {
                LOG.Error(e.StackTrace, this.GetType(), e);
            }
        }

        public async Task OnDisconnect(long accountId)
        {
            var baseUser = _playService.FindUser(accountId);

            if (baseUser != null)
            {
                await this._stage!.OnDisconnect(baseUser.Actor);
            }
            else
            {
                LOG.Error($"user is not exist : {accountId}", this.GetType());
            }
        }



    }





}
