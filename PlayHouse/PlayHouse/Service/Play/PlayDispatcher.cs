using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Play;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Play.Base;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;
using System.Collections.Concurrent;

namespace PlayHouse.Service.Play;

internal interface IPlayDispatcher
{
    public void Post(RoutePacket routePacket);
}

internal class PlayDispatcher : IPlayDispatcher
{
    private readonly LOG<PlayDispatcher> _log = new();
    private readonly ConcurrentDictionary<string, BaseActor> _baseUsers = new();
    private readonly ConcurrentDictionary<string, BaseStage> _baseRooms = new();

    private readonly ushort _serviceId;
    private readonly IClientCommunicator _clientCommunicator;
    private readonly RequestCache _requestCache;
    private readonly IServerInfoCenter _serverInfoCenter;
    private readonly string _publicEndpoint;
    private readonly TimerManager _timerManager;
    private readonly XSender _sender;
    private readonly PlayOption _playOption;

    public PlayDispatcher(
        ushort serviceId, 
        IClientCommunicator clientCommunicator, 
        RequestCache requestCache, 
        IServerInfoCenter serverInfoCenter, 
        string publicEndpoint, 
        PlayOption playOption)
    {
        _serviceId = serviceId;
        _clientCommunicator = clientCommunicator;
        _requestCache = requestCache;
        _serverInfoCenter = serverInfoCenter;
        _publicEndpoint = publicEndpoint;

        _timerManager = new TimerManager(this);
        _sender = new XSender(serviceId, clientCommunicator, requestCache);
        _playOption = playOption;
    }

    public void RemoveRoom(string stageId)
    {
        _baseRooms.Remove(stageId, out _);
    }

    public void RemoveUser(string accountId)
    {
        _baseUsers.Remove(accountId, out _);
    }

    private string Endpoint()
    {
        return _publicEndpoint;
    }


    private BaseStage MakeBaseRoom(string stageId)
    {
        var stageSender = new XStageSender(_serviceId, stageId, this, _clientCommunicator, _requestCache);
        var sessionUpdator = new XSessionUpdater(Endpoint(), stageSender);
        var baseStage = new BaseStage(stageId, this, _clientCommunicator, _requestCache, _serverInfoCenter, sessionUpdator, stageSender);
        _baseRooms[stageId] = baseStage;
        return baseStage;
    }

    public BaseActor? FindUser(string accountId)
    {
        if (_baseUsers.TryGetValue(accountId, out var user)) return user;
        return null;
    }

    public void AddUser(BaseActor baseActor)
    {

        _baseUsers[baseActor.ActorSender.AccountId()] = baseActor;
    }

    public BaseStage? FindRoom(string stageId)
    {
        return _baseRooms[stageId];
    }

    public void CancelTimer(string stageId, long timerId)
    {
        if (_baseRooms.TryGetValue(stageId, out var room))
        {
            room.CancelTimer(timerId);
        }
    }

    public IStage CreateContentRoom(string stageType, XStageSender roomSender)
    {
        return _playOption.PlayProducer.GetStage(stageType, roomSender);
    }

    public IActor CreateContentUser(string stageType, XActorSender userSender)
    {
        return _playOption.PlayProducer.GetActor(stageType, userSender);
    }

    public bool IsValidType(string stageType)
    {
        return _playOption.PlayProducer.IsInvalidType(stageType);
    }

    private async Task DoBaseRoomPacket(int msgId, RoutePacket routePacket, string stageId)
    {

        if (msgId == CreateStageReq.Descriptor.Index)
        {
            var newStageId = routePacket.StageId;
            if (_baseRooms.ContainsKey(newStageId))
            {
                _sender.Reply((ushort)BaseErrorCode.AlreadyExistStage);
            }
            else
            {
                await MakeBaseRoom(newStageId).Send(routePacket);
            }
        }
        else if (msgId == CreateJoinStageReq.Descriptor.Index)
        {
            _baseRooms.TryGetValue(stageId, out var room);
            if (room != null)
            {
                await room!.Send(routePacket);
            }
            else
            {
                await MakeBaseRoom(stageId).Send(routePacket);
            }
        }
        else if (msgId == TimerMsg.Descriptor.Index)
        {
            var timerId = routePacket.TimerId;
            var protoPayload = (routePacket.Payload as ProtoPayload)!;
            TimerProcess(stageId, timerId, (protoPayload.GetProto() as TimerMsg)!, routePacket.TimerCallback!);
        }
        else if (msgId == DestroyStage.Descriptor.Index)
        {
            _baseRooms.Remove(stageId, out _);
        }
        else
        {
            if (!_baseRooms.TryGetValue(stageId, out var room))
            {
                if (msgId == StageTimer.Descriptor.Index) return;
                _log.Error(() => $"Room is not exist : {stageId},{msgId}");
                _sender.Reply((ushort)BaseErrorCode.StageIsNotExist);
                return;
            }

            if (msgId == JoinStageReq.Descriptor.Index ||
                msgId == StageTimer.Descriptor.Index ||
                msgId == DisconnectNoticeMsg.Descriptor.Index ||
                msgId == AsyncBlock.Descriptor.Index)
            {
                await room!.Send(routePacket);
            }
            else
            {
                _log.Error(() => $"message is not base packet - [msgId:{msgId}]");
            }
        }
    }

    private void TimerProcess(string stageId, long timerId, TimerMsg timerMsg, TimerCallbackTask timerCallback)
    {
        _baseRooms.TryGetValue(stageId, out var room);

        if (room != null)
        {
            if (timerMsg.Type == TimerMsg.Types.Type.Repeat)
            {

                _timerManager.RegisterRepeatTimer(
                    stageId,
                    timerId,
                    timerMsg.InitialDelay,
                    timerMsg.Period,
                    timerCallback);
            }
            else if (timerMsg.Type == TimerMsg.Types.Type.Count)
            {
                _timerManager.RegisterCountTimer(
                      stageId,
                      timerId,
                      timerMsg.InitialDelay,
                      timerMsg.Count,
                      timerMsg.Period,
                      timerCallback);
            }
            else if (timerMsg.Type == TimerMsg.Types.Type.Cancel)
            {
                _timerManager.CancelTimer(timerId);
            }
            else
            {
                _log.Error(() => $"Invalid timer type - [timerType:{timerMsg.Type}]");
            }

        }
        else
        {
            _log.Debug(() => $"Stage for timer is not exist - [stageId:{stageId}, timerType:{timerMsg.Type}]");
        }
    }

    public void Post(RoutePacket routePacket)
    {
        Task.Run(async () => await DispatchAsync(routePacket));
    }

    public async Task  DispatchAsync(RoutePacket routePacket)
    {
        using (routePacket)
        {
            var msgId = routePacket.MsgId;
            var isBase = routePacket.IsBase();
            var stageId = routePacket.RouteHeader.StageId;

            var roomPacket = routePacket;
            if (routePacket.Payload is not EmptyPayload)
            {
                roomPacket = RoutePacket.MoveOf(routePacket);
            }
            if (isBase)
            {
                
                await DoBaseRoomPacket(msgId, roomPacket, stageId);
                
            }
            else
            {
                
                _baseRooms.TryGetValue(stageId, out var baseStage);
                if (baseStage != null)
                {
                    //PacketContext.AsyncCore.Init();
                    //ServiceAsyncContext.Init();
                    await baseStage.Send(roomPacket);
                    //PacketContext.AsyncCore.Clear();
                    //ServiceAsyncContext.Clear();

                }
                else
                {
                    _log.Error(() => $"stage is not exist - [stageId:{stageId},msgName:{msgId}]");
                }
                
            }
        }
    }

    internal int GetActorCount()
    {
        return _baseUsers.Count;
    }
}
