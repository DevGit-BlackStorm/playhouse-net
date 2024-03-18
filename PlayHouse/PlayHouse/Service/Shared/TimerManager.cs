using PlayHouse.Communicator.Message;
using System.Collections.Concurrent;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Play;

namespace PlayHouse.Service.Shared;

internal class TimerManager
{
    private readonly IPlayDispatcher _dispatcher;
    private readonly ConcurrentDictionary<long, Timer> _timers = new ConcurrentDictionary<long, Timer>();

    public TimerManager(IPlayDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public long RegisterRepeatTimer(string stageId, long timerId, long initialDelay, long period, TimerCallbackTask timerCallback)
    {
        var timer = new Timer(timerState =>
        {
            var routePacket = RoutePacket.StageTimerOf(stageId, timerId, timerCallback, timerState);
            _dispatcher.OnPost(routePacket);
        }, null, initialDelay, period);


        _timers[timerId] = timer;
        return timerId;
    }

    public long RegisterCountTimer(string stageId, long timerId, long initialDelay, int count, long period, TimerCallbackTask timerCallback)
    {
        int remainingCount = count;

        var timer = new Timer(timerState =>
        {
            if (remainingCount > 0)
            {
                var routePacket = RoutePacket.StageTimerOf(stageId, timerId, timerCallback, timerState);
                _dispatcher.OnPost(routePacket);
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
        if (_timers.TryGetValue(timerId, out Timer? timer))
        {
            timer.Dispose();
            _timers.Remove(timerId, out _);
        }
    }
}
