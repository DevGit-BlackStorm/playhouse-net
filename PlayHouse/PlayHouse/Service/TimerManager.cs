using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System.Collections.Concurrent;
using PlayHouse.Production;
    
namespace PlayHouse.Service;

public class TimerManager
{
    private readonly IProcessor _service;
    private readonly ConcurrentDictionary<long, Timer> _timers = new ConcurrentDictionary<long, Timer>();

    public TimerManager(IProcessor service)
    {
        _service = service;
    }

    public long RegisterRepeatTimer(string stageId, long timerId, long initialDelay, long period, TimerCallbackTask timerCallback)
    {
        var timer = new Timer(timerState =>
        {
            var routePacket = RoutePacket.StageTimerOf(stageId, timerId, timerCallback, timerState);
            _service.OnReceive(routePacket);
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
                 _service.OnReceive(routePacket);
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
            _timers.Remove(timerId,out _);
        }
    }
}
