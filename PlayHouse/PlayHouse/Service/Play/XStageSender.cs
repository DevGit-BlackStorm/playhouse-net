using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Play;

internal class XStageSender(
    ushort serviceId,
    long stageId,
    IPlayDispatcher dispatcher,
    IClientCommunicator clientCommunicator,
    RequestCache reqCache)
    : XSender(serviceId, clientCommunicator, reqCache), IStageSender
{
    private readonly HashSet<long> _timerIds = new();

    public long StageId { get; } = stageId;

    public string StageType { get; private set; } = "";

    public long AddRepeatTimer(TimeSpan initialDelay, TimeSpan period, TimerCallbackTask timerCallback)
    {
        var timerId = MakeTimerId();
        var packet = RoutePacket.AddTimerOf(
            TimerMsg.Types.Type.Repeat,
            StageId,
            timerId,
            timerCallback,
            initialDelay,
            period
        );
        dispatcher.OnPost(packet);
        _timerIds.Add(timerId);
        return timerId;
    }

    public long AddCountTimer(TimeSpan initialDelay, int count, TimeSpan period, TimerCallbackTask timerCallback)
    {
        var timerId = MakeTimerId();
        var packet = RoutePacket.AddTimerOf(
            TimerMsg.Types.Type.Count,
            StageId,
            timerId,
            timerCallback,
            initialDelay,
            period,
            count
        );
        dispatcher.OnPost(packet);
        _timerIds.Add(timerId);
        return timerId;
    }

    public void CancelTimer(long timerId)
    {
        var packet = RoutePacket.AddTimerOf(
            TimerMsg.Types.Type.Cancel,
            StageId,
            timerId,
            () => Task.CompletedTask,
            TimeSpan.Zero,
            TimeSpan.Zero
        );
        dispatcher.OnPost(packet);
        _timerIds.Remove(timerId);
    }

    public void CloseStage()
    {
        foreach (var timerId in _timerIds)
        {
            var packet = RoutePacket.AddTimerOf(
                TimerMsg.Types.Type.Cancel,
                StageId,
                timerId,
                () => Task.CompletedTask,
                TimeSpan.Zero,
                TimeSpan.Zero
            );
            dispatcher.OnPost(packet);
        }

        _timerIds.Clear();

        var packet2 = RoutePacket.StageOf(StageId, 0, RoutePacket.Of(DestroyStage.Descriptor.Name, new EmptyPayload()),
            true, false);
        dispatcher.OnPost(packet2);
    }

    public override void SendToClient(string sessionNid, long sid, IPacket packet)
    {
        PacketContext.AsyncCore.Add(SendTarget.Client, 0, packet);

        var routePacket = RoutePacket.ClientOf(ServiceId, sid, packet, StageId);
        ClientCommunicator.Send(sessionNid, routePacket);
    }

    public void AsyncBlock(AsyncPreCallback preCallback, AsyncPostCallback? postCallback = null)
    {
        Task.Run(async () =>
        {
            var result = await preCallback.Invoke();
            if (postCallback != null)
            {
                var packet = AsyncBlockPacket.Of(StageId, postCallback, result!);
                dispatcher.OnPost(packet);
            }
        });
    }


    private long MakeTimerId()
    {
        return TimerIdMaker.MakeId();
    }

    public bool HasTimer(long timerId)
    {
        return _timerIds.Contains(timerId);
    }

    public void SetStageType(string stageType)
    {
        StageType = stageType;
    }
}