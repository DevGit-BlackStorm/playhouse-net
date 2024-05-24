using System.Collections.Concurrent;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Play;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;
using PlayHouse.Service.Play.Base.Command;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Service.Play.Base;

internal class BaseStage
{
    private readonly PlayDispatcher _dispatcher;
    private readonly AtomicBoolean _isUsing = new(false);
    private readonly LOG<BaseStage> _log = new();
    private readonly BaseStageCmdHandler _msgHandler = new();
    private readonly ConcurrentQueue<RoutePacket> _msgQueue = new();
    private readonly IServerInfoCenter _serverInfoCenter;
    private readonly ISessionUpdater _sessionUpdater;
    private readonly long _stageId;

    private IStage? _stage;

    public BaseStage(long stageId,
        PlayDispatcher dispatcher,
        IClientCommunicator clientCommunicator,
        RequestCache reqCache,
        IServerInfoCenter serverInfoCenter,
        ISessionUpdater sessionUpdater,
        XStageSender stageSender)
    {
        _stageId = stageId;
        _dispatcher = dispatcher;
        _serverInfoCenter = serverInfoCenter;
        StageSender = stageSender;
        _sessionUpdater = sessionUpdater;


        _msgHandler.Register(CreateStageReq.Descriptor.Index, new CreateStageCmd(dispatcher));
        _msgHandler.Register(JoinStageReq.Descriptor.Index, new JoinStageCmd(dispatcher));
        _msgHandler.Register(CreateJoinStageReq.Descriptor.Index, new CreateJoinStageCmd(dispatcher));
        _msgHandler.Register(StageTimer.Descriptor.Index, new StageTimerCmd());
        _msgHandler.Register(DisconnectNoticeMsg.Descriptor.Index, new DisconnectNoticeCmd());
        _msgHandler.Register(AsyncBlock.Descriptor.Index, new AsyncBlockCmd());
    }

    public XStageSender StageSender { get; }

    public bool IsCreated { get; private set; }

    public long StageId => StageSender.StageId;

    private async Task Dispatch(RoutePacket routePacket)
    {
        StageSender.SetCurrentPacketHeader(routePacket.RouteHeader);
        try
        {
            if (routePacket.IsBase())
            {
                await _msgHandler.Dispatch(this, routePacket);
            }
            else
            {
                var accountId = routePacket.AccountId;
                var baseUser = _dispatcher.FindUser(accountId);
                if (baseUser != null)
                {
                    await _stage!.OnDispatch(baseUser.Actor, CPacket.Of(routePacket.MsgId, routePacket.Payload));
                }
            }
        }
        catch (Exception e)
        {
            StageSender.Reply((ushort)BaseErrorCode.SystemError);
            _log.Error(() => e.ToString());
        }
        finally
        {
            StageSender.ClearCurrentPacketHeader();
        }
    }

    public void Post(RoutePacket routePacket)
    {
        _msgQueue.Enqueue(routePacket);
        if (_isUsing.CompareAndSet(false, true))
        {
            Task.Run(async () =>
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
                        StageSender.Reply((ushort)BaseErrorCode.UncheckedContentsError);
                        _log.Error(() => e.ToString());
                    }
                }

                _isUsing.Set(false);
            });
        }
    }

    public async Task<(ushort errorCode, IPacket reply)> Create(string stageType, IPacket packet)
    {
        _stage = _dispatcher.CreateContentRoom(stageType, StageSender);
        StageSender.SetStageType(stageType);
        var outcome = await _stage.OnCreate(packet);
        IsCreated = true;
        return outcome;
    }


    public async Task<(ushort errorCode, IPacket reply)> Join(long accountId, string sessionEndpoint, int sid,
        string apiEndpoint, IPacket packet)
    {
        var baseUser = _dispatcher.FindUser(accountId);

        if (baseUser == null)
        {
            var userSender = new XActorSender(accountId, sessionEndpoint, sid, apiEndpoint, this, _serverInfoCenter);
            var user = _dispatcher.CreateContentUser(StageSender.StageType, userSender);
            baseUser = new BaseActor(user, userSender);
            await baseUser.Actor.OnCreate();
            _dispatcher.AddUser(baseUser);
        }
        else
        {
            baseUser.ActorSender.Update(sessionEndpoint, sid, apiEndpoint);
        }

        var (errorCode, reply) = await _stage!.OnJoinStage(baseUser.Actor, packet);

        if (errorCode != (ushort)BaseErrorCode.Success)
        {
            _dispatcher.RemoveUser(accountId);
        }
        else
        {
            await _sessionUpdater.UpdateStageInfo(sessionEndpoint, sid);
        }

        return (errorCode, reply);
    }


    public void Reply(ushort errorCode)
    {
        StageSender.Reply(errorCode);
    }

    public void Reply(IPacket packet)
    {
        StageSender.Reply(packet);
    }

    public void LeaveStage(long accountId, string sessionEndpoint, int sid)
    {
        _dispatcher.RemoveUser(accountId);
        var request = new LeaveStageMsg();
        request.StageId = _stageId;
        StageSender.SendToBaseSession(sessionEndpoint, sid, RoutePacket.Of(request));
    }

    public void CancelTimer(long timerId)
    {
        StageSender.CancelTimer(timerId);
    }

    public bool HasTimer(long timerId)
    {
        return StageSender.HasTimer(timerId);
    }

    public async Task OnPostCreate()
    {
        try
        {
            await _stage!.OnPostCreate();
        }
        catch (Exception e)
        {
            _log.Error(() => e.ToString());
        }
    }

    public async Task OnPostJoinRoom(long accountId)
    {
        try
        {
            var baseUser = _dispatcher.FindUser(accountId);

            if (baseUser != null)
            {
                await _stage!.OnPostJoinStage(baseUser.Actor);
            }
            else
            {
                _log.Error(() => $"user is not exist - [accountId:{accountId}]");
            }
        }
        catch (Exception e)
        {
            _log.Error(() => e.ToString());
        }
    }

    public async Task OnDisconnect(long accountId)
    {
        var baseUser = _dispatcher.FindUser(accountId);

        if (baseUser != null)
        {
            await _stage!.OnDisconnect(baseUser.Actor);
        }
        else
        {
            _log.Error(() => $"user is not exist - [accountId:{accountId}]");
        }
    }
}