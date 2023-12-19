using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Production;

namespace PlayHouse.Service.Play
{
    internal class XStageSender : XSender, IStageSender
    {
        private readonly string _stageId;
        private readonly PlayProcessor _playProcessor;
        private readonly HashSet<long> _timerIds = new();
        private string _stateType = "";

        public XStageSender(ushort serviceId, string stageId, PlayProcessor playProcessor,IClientCommunicator clientCommunicator, RequestCache reqCache) : base(serviceId, clientCommunicator, reqCache)
        {
            _stageId = stageId;
            _playProcessor = playProcessor;
        }

        public string StageId => _stageId;
        public string StageType => _stateType;


        private long MakeTimerId()
        {
            return TimerIdMaker.MakeId();
        }

        public long AddRepeatTimer(TimeSpan initialDelay, TimeSpan period, TimerCallbackTask timerCallback)
        {
            var timerId = MakeTimerId();
            var packet = RoutePacket.AddTimerOf(
                TimerMsg.Types.Type.Repeat,
                _stageId,
                timerId,
                timerCallback,
                initialDelay,
                period
            );
            _playProcessor.OnReceive(packet);
            _timerIds.Add(timerId);
            return timerId;
        }

        public long AddCountTimer(TimeSpan initialDelay, int count, TimeSpan period, TimerCallbackTask timerCallback)
        {
            var timerId = MakeTimerId();
            var packet = RoutePacket.AddTimerOf(
                TimerMsg.Types.Type.Count,
                _stageId,
                timerId,
                timerCallback,
                initialDelay,
                period,
                count
            );
            _playProcessor.OnReceive(packet);
            _timerIds.Add(timerId);
            return timerId;
        }

        public void CancelTimer(long timerId)
        {
            var packet = RoutePacket.AddTimerOf(
                TimerMsg.Types.Type.Cancel,
                _stageId,
                timerId,
                () => { return Task.CompletedTask; },
                TimeSpan.Zero,
                TimeSpan.Zero
            );
            _playProcessor.OnReceive(packet);
            _timerIds.Remove(timerId);
        }

        public void CloseStage()
        {
            foreach (var timerId in _timerIds)
            {
                var packet = RoutePacket.AddTimerOf(
                    TimerMsg.Types.Type.Cancel,
                    _stageId,
                    timerId,
                    () => { return Task.CompletedTask; },
                    TimeSpan.Zero,
                    TimeSpan.Zero
                );
                _playProcessor.OnReceive(packet);
            }
            _timerIds.Clear();

            var packet2 = RoutePacket.StageOf(_stageId, string.Empty,new Packet(DestroyStage.Descriptor.Index), true, false);
            _playProcessor.OnReceive(packet2);
        }

        public void AsyncBlock(AsyncPreCallback preCallback, AsyncPostCallback? postCallback = null)
        {
            Task.Run(async () =>
            {
                var result = await preCallback.Invoke();
                if (postCallback != null)
                {
                    var packet = AsyncBlockPacket.Of(_stageId,  postCallback, result!);
                    _playProcessor.OnReceive(packet);
                }
            });
            
        }

        public bool HasTimer(long timerId) => _timerIds.Contains(timerId);

        public  void SetStageType(string stageType)
        {
            _stateType = stageType;
        }
    }

}
