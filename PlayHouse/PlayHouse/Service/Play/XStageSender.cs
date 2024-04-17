using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Service.Shared;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play
{
    internal class XStageSender : XSender, IStageSender
    {
        private readonly long _stageId;
        private readonly IPlayDispatcher _dispatcher;
        private readonly HashSet<long> _timerIds = new();
        private string _stateType = "";

        public XStageSender(
            ushort serviceId, 
            long stageId,
            IPlayDispatcher dispatcher,
            IClientCommunicator clientCommunicator, 
            RequestCache reqCache) : base(serviceId, clientCommunicator, reqCache)
        {
            _stageId = stageId;
            _dispatcher = dispatcher;
        }

        public long StageId => _stageId;
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
            _dispatcher.OnPost(packet);
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
            _dispatcher.OnPost(packet);
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
            _dispatcher.OnPost(packet);
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
                _dispatcher.OnPost(packet);
            }
            _timerIds.Clear();

            var packet2 = RoutePacket.StageOf(_stageId, 0, RoutePacket.Of(DestroyStage.Descriptor.Index,new EmptyPayload()), true, false);
            _dispatcher.OnPost(packet2);
        }

        override public void SendToClient(string sessionEndpoint, int sid, IPacket packet)
        {
            PacketContext.AsyncCore.Add(SendTarget.Client, 0, packet);

            RoutePacket routePacket = RoutePacket.ClientOf(_serviceId, sid, packet,_stageId);
            _clientCommunicator.Send(sessionEndpoint, routePacket);
        }

        public void AsyncBlock(AsyncPreCallback preCallback, AsyncPostCallback? postCallback = null)
        {
            Task.Run(async () =>
            {
                var result = await preCallback.Invoke();
                if (postCallback != null)
                {
                    var packet = AsyncBlockPacket.Of(_stageId,  postCallback, result!);
                    _dispatcher.OnPost(packet);
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
