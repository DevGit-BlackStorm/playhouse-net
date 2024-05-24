using System.Collections.Concurrent;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Play;

namespace PlayHouse.Service.Shared;

internal class TimerManager(IPlayDispatcher dispatcher)
{
    private readonly ConcurrentDictionary<long, Timer> _timers = new();

    public long RegisterRepeatTimer(long stageId, long timerId, long initialDelay, long period,
        TimerCallbackTask timerCallback)
    {
        var timer = new Timer(timerState =>
        {
            var routePacket = RoutePacket.StageTimerOf(stageId, timerId, timerCallback, timerState);
            dispatcher.OnPost(routePacket);
        }, null, initialDelay, period);


        _timers[timerId] = timer;
        return timerId;
    }

    public long RegisterCountTimer(long stageId, long timerId, long initialDelay, int count, long period,
        TimerCallbackTask timerCallback)
    {
        var remainingCount = count;

        var timer = new Timer(timerState =>
        {
            if (remainingCount > 0)
            {
                var routePacket = RoutePacket.StageTimerOf(stageId, timerId, timerCallback, timerState);
                dispatcher.OnPost(routePacket);
                remainingCount--;
            }
            else
            {
                CancelTimer(timerId);
            }
        }, null, initialDelay, period);

        _timers[timerId] = timer;
        return timerId;
    }

    public void CancelTimer(long timerId)
    {
        if (_timers.TryGetValue(timerId, out var timer))
        {
            timer.Dispose();
            _timers.Remove(timerId, out _);
        }
    }
}