using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Utils;
using PlayHouse.Communicator;
using System.Collections.Concurrent;
using PlayHouse.Service.Play.Base.Command;
using PlayHouse.Production.Play;
using PlayHouse.Service.Shared;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play.Base;
internal class BaseStage
{
    private readonly LOG<BaseStage> _log = new ();
    private readonly string _stageId;
    private readonly PlayDispatcher _dispatcher;
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
                     PlayDispatcher dispatcher, 
                     IClientCommunicator clientCommunicator,
                     RequestCache reqCache, 
                     IServerInfoCenter serverInfoCenter,
                     ISessionUpdater sessionUpdater,
                     XStageSender stageSender )
    {
        _stageId = stageId;
        _dispatcher = dispatcher;
        _serverInfoCenter = serverInfoCenter;
        _stageSender = stageSender;
        _sessionUpdater = sessionUpdater;


        _msgHandler.Register(CreateStageReq.Descriptor.Index, new CreateStageCmd(dispatcher));
        _msgHandler.Register(JoinStageReq.Descriptor.Index, new JoinStageCmd(dispatcher));
        _msgHandler.Register(CreateJoinStageReq.Descriptor.Index, new CreateJoinStageCmd(dispatcher));
        _msgHandler.Register(StageTimer.Descriptor.Index, new StageTimerCmd());
        _msgHandler.Register(DisconnectNoticeMsg.Descriptor.Index, new DisconnectNoticeCmd());
        _msgHandler.Register(AsyncBlock.Descriptor.Index, new AsyncBlockCmd());
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
                var baseUser = _dispatcher.FindUser(accountId);
                if (baseUser != null)
                {
                    await _stage!.OnDispatch(baseUser.Actor,CPacket.Of(routePacket.MsgId, routePacket.Payload));
                }
            }
        }
        catch (Exception e)
        {
            _stageSender.Reply((ushort)BaseErrorCode.SystemError);
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
                        _stageSender.Reply((ushort)BaseErrorCode.UncheckedContentsError);
                        _log.Error(()=>e.ToString());
                    }
                }
         
                _isUsing.Set(false);
         
        }
    }

    public async Task<(ushort errorCode, IPacket reply)> Create(string stageType, IPacket packet)
    {
        _stage = _dispatcher.CreateContentRoom(stageType, _stageSender);
        _stageSender.SetStageType(stageType);
        var outcome = await _stage.OnCreate(packet);
        IsCreated = true;
        return outcome;
    }


    public async Task<(ushort errorCode,IPacket reply,int stageKey)> Join(string accountId, string sessionEndpoint, int sid, string apiEndpoint, IPacket packet)
    {
        BaseActor? baseUser = _dispatcher.FindUser(accountId);

        if (baseUser == null)
        {
            XActorSender userSender = new XActorSender(accountId, sessionEndpoint, sid, apiEndpoint, this, _serverInfoCenter);
            IActor user = _dispatcher.CreateContentUser(_stageSender.StageType, userSender);
            baseUser = new BaseActor(user, userSender);
            await baseUser.Actor.OnCreate();
            _dispatcher.AddUser(baseUser);
        }
        else
        {
            baseUser.ActorSender.Update(sessionEndpoint, sid, apiEndpoint);
        }

        var outcome = await _stage!.OnJoinStage(baseUser.Actor, packet);
        int stageKey = 0;

        if (outcome.errorCode != (ushort)BaseErrorCode.Success)
        {
            _dispatcher.RemoveUser(accountId);
        }
        else
        {
            stageKey = await _sessionUpdater.UpdateStageInfo(sessionEndpoint, sid);
        }

        return  (outcome.errorCode, outcome.reply, stageKey);
    }



    public void Reply(ushort errorCode)
    {
        this._stageSender.Reply(errorCode);
    }
    public void Reply(IPacket packet)
    {
        this._stageSender.Reply(packet);
    }

    public void LeaveStage(string accountId, string sessionEndpoint, int sid)
    {
        this._dispatcher.RemoveUser(accountId);
        var request = new LeaveStageMsg();
        request.StageId = _stageId;
        this._stageSender.SendToBaseSession(sessionEndpoint, sid,RoutePacket.Of(request));
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
            BaseActor? baseUser = _dispatcher.FindUser(accountId);

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
        var baseUser = _dispatcher.FindUser(accountId);

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





