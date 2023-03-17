using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    using PlayHouse.Communicator.Message;
    using PlayHouse.Communicator;
    using System;
    using System.Collections.Generic;
    using System.Threading;


    public class TimerManager
    {
        private readonly IService _service;
        private readonly Dictionary<long, Timer> _timers = new Dictionary<long, Timer>();

        public TimerManager(IService service)
        {
            _service = service;
        }

        public void Start()
        {
            //var timer = new Timer(async state =>
            //{
            //    // Log room state here
            //    await Task.CompletedTask;
            //}, null, 1000, 5000);
            //_timers[0] = timer;
        }

        public long RegisterRepeatTimer(long stageId, long timerId, long initialDelay, long period, TimerCallback timerCallback)
        {
            var timer = new Timer(state =>
            {
                var routePacket = RoutePacket.StageTimerOf(stageId, timerId, timerCallback);
                _service.OnReceive(routePacket);
            }, null, initialDelay, period);
            

            _timers[timerId] = timer;
            return timerId;
        }

        public long RegisterCountTimer(long stageId, long timerId, long initialDelay, int count, long period, TimerCallback timerCallback)
        {
            int remainingCount = count;

            var timer = new Timer(state =>
            {
                if (remainingCount > 0)
                {
                    var routePacket = RoutePacket.StageTimerOf(stageId, timerId, timerCallback);
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
                _timers.Remove(timerId);
            }
        }
    }

}
