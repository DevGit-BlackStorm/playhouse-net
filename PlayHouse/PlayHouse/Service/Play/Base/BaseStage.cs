using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Utils;
using PlayHouse.Communicator;
using System.Collections.Concurrent;
using static NetMQ.NetMQSelector;
using PlayHouse.Service.Api;
using PlayHouse.Service.Play.Base.Command;
using PlayHouse.Production;
using PlayHouse.Production.Play;

namespace PlayHouse.Service.Play.Base
{
    public class BaseStage
    {
        private Guid _stageId;
        private PlayProcessor _playProcessor;
        private IClientCommunicator _clientCommunicator;
        private RequestCache _reqCache;
        private IServerInfoCenter _serverInfoCenter;
        private XStageSender _stageSender;
        private BaseStageCmdHandler _msgHandler = new BaseStageCmdHandler();
        private ConcurrentQueue<RoutePacket> _msgQueue = new ConcurrentQueue<RoutePacket>();
        private AtomicBoolean _isUsing = new AtomicBoolean(false);
        private ISessionUpdater _sessionUpdater;

        public XStageSender StageSender => _stageSender;

        private IStage?  _stage; 
        public bool IsCreated { get; private set; }

        public BaseStage(Guid stageId, 
                         PlayProcessor playProcessor, 
                         IClientCommunicator clientCommunicator,
                         RequestCache reqCache, 
                         IServerInfoCenter serverInfoCenter,
                         ISessionUpdater sessionUpdater,
                         XStageSender stageSender )
        {
            _stageId = stageId;
            _playProcessor = playProcessor;
            _clientCommunicator = clientCommunicator;
            _reqCache = reqCache;
            _serverInfoCenter = serverInfoCenter;
            _stageSender = stageSender;
            _sessionUpdater = sessionUpdater;


            _msgHandler.Register(CreateStageReq.Descriptor.Index, new CreateStageCmd(playProcessor));
            _msgHandler.Register(JoinStageReq.Descriptor.Index, new JoinStageCmd(playProcessor));
            _msgHandler.Register(CreateJoinStageReq.Descriptor.Index, new CreateJoinStageCmd(playProcessor));
            _msgHandler.Register(StageTimer.Descriptor.Index, new StageTimerCmd(playProcessor));
            _msgHandler.Register(DisconnectNoticeMsg.Descriptor.Index, new DisconnectNoticeCmd(playProcessor));
            _msgHandler.Register(AsyncBlock.Descriptor.Index, new AsyncBlockCmd(playProcessor));
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
                    Guid accountId = routePacket.AccountId;
                    var baseUser = _playProcessor.FindUser(accountId);
                    if (baseUser != null)
                    {
                        await _stage!.OnDispatch(baseUser.Actor, new Packet(routePacket.MsgId, routePacket.MovePayload()));
                    }
                }
            }
            catch (Exception e)
            {
                _stageSender.ErrorReply(routePacket.RouteHeader,(ushort) BaseErrorCode.SystemError);
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
                            _stageSender.ErrorReply(routePacket.RouteHeader, (ushort)BaseErrorCode.UncheckedContentsError);
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
            _stage = _playProcessor.CreateContentRoom(stageType, _stageSender);
            _stageSender.SetStageType(stageType);
            var outcome = await _stage.OnCreate(packet);
            IsCreated = true;
            return outcome;
        }

        public async Task<(ReplyPacket, int)> Join(Guid accountId, string sessionEndpoint, int sid, string apiEndpoint, Packet packet)
        {
            BaseActor? baseUser = _playProcessor.FindUser(accountId);

            if (baseUser == null)
            {
                XActorSender userSender = new XActorSender(accountId, sessionEndpoint, sid, apiEndpoint, this, _serverInfoCenter);
                IActor user = _playProcessor.CreateContentUser(_stageSender.StageType, userSender);
                baseUser = new BaseActor(user, userSender);
                await baseUser.Actor.OnCreate();
                _playProcessor.AddUser(baseUser);
            }
            else
            {
                baseUser.ActorSender.Update(sessionEndpoint, sid, apiEndpoint);
            }

            var outcome = await _stage!.OnJoinStage(baseUser.Actor, packet);
            int stageIndex = 0;

            if (!outcome.IsSuccess())
            {
                _playProcessor.RemoveUser(accountId);
            }
            else
            {
                stageIndex = await _sessionUpdater.UpdateStageInfo(sessionEndpoint, sid);
            }

            return new (outcome, stageIndex);
        }



        public void Reply(ReplyPacket packet)
        {
            this._stageSender.Reply(packet);
        }

        public void LeaveStage(Guid accountId, string sessionEndpoint, int sid)
        {
            this._playProcessor.RemoveUser(accountId);
            var request = new LeaveStageMsg();
            this._stageSender.SendToBaseSession(sessionEndpoint, sid,new Packet(request));
        }

        public Guid StageId => _stageSender.StageId;

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

        public async Task OnPostJoinRoom(Guid accountId)
        {
            try
            {
                BaseActor? baseUser = _playProcessor.FindUser(accountId);

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

        public async Task OnDisconnect(Guid accountId)
        {
            var baseUser = _playProcessor.FindUser(accountId);

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
