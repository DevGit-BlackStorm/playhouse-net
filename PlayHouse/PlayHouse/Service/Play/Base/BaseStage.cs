using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Utils;
using PlayHouse.Communicator;
using System.Collections.Concurrent;
using PlayHouse.Service.Play.Base.Command;
using PlayHouse.Production;
using PlayHouse.Production.Play;
using Google.Protobuf;

namespace PlayHouse.Service.Play.Base;
public class BaseStage
{
    private readonly LOG<BaseStage> _log = new ();
    private readonly string _stageId;
    private readonly PlayProcessor _playProcessor;
    private IClientCommunicator _clientCommunicator;
    private RequestCache _reqCache;
    private readonly IServerInfoCenter _serverInfoCenter;
    private readonly XStageSender _stageSender;
    private readonly BaseStageCmdHandler _msgHandler = new BaseStageCmdHandler();
    private readonly ConcurrentQueue<RoutePacket> _msgQueue = new ConcurrentQueue<RoutePacket>();
    private readonly AtomicBoolean _isUsing = new AtomicBoolean(false);
    private readonly ISessionUpdater _sessionUpdater;

    public XStageSender StageSender => _stageSender;

    private IStage?  _stage; 
    public bool IsCreated { get; private set; }

    public BaseStage(string stageId, 
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
                string accountId = routePacket.AccountId;
                var baseUser = _playProcessor.FindUser(accountId);
                if (baseUser != null)
                {
                    await _stage!.OnDispatch(baseUser.Actor, new XPacket(routePacket.MsgId, routePacket.Payload));
                }
            }
        }
        catch (Exception e)
        {
            _stageSender.ErrorReply(routePacket.RouteHeader,(ushort) BaseErrorCode.SystemError);
            _log.Error(()=>e.ToString());
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
         
                while (_msgQueue.TryDequeue(out var item))
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
                        _log.Error(()=>e.ToString());
                    }
                }
         
                _isUsing.Set(false);
         
        }
    }

    public async Task<ReplyPacket> Create(string stageType, Packet packet)
    {
        _stage = _playProcessor.CreateContentRoom(stageType, _stageSender);
        _stageSender.SetStageType(stageType);
        var outcome = await _stage.OnCreate(packet.ToXPacket());
        IsCreated = true;
        return outcome;
    }


    public async Task<(ReplyPacket, int)> Join(string accountId, string sessionEndpoint, int sid, string apiEndpoint, Packet packet)
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

        var outcome = await _stage!.OnJoinStage(baseUser.Actor, packet.ToXPacket());
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

    public void LeaveStage(string accountId, string sessionEndpoint, int sid)
    {
        this._playProcessor.RemoveUser(accountId);
        var request = new LeaveStageMsg();
        request.StageId = _stageId;
        this._stageSender.SendToBaseSession(sessionEndpoint, sid,new Packet(request));
    }

    public string StageId => _stageSender.StageId;

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
            _log.Error(()=>e.ToString());
        }
    }

    public async Task OnPostJoinRoom(string accountId)
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
                _log.Error(()=>$"user is not exist - [accountId:{accountId}]");
            }

        }
        catch (Exception e)
        {
            _log.Error(()=>e.ToString());
        }
    }

    public async Task OnDisconnect(string accountId)
    {
        var baseUser = _playProcessor.FindUser(accountId);

        if (baseUser != null)
        {
            await this._stage!.OnDisconnect(baseUser.Actor);
        }
        else
        {
            _log.Error(()=>$"user is not exist - [accountId:{accountId}]");
        }
    }
}





