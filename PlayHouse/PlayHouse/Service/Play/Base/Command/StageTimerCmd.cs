using PlayHouse.Communicator.Message;
using PlayHouse.Utils;

namespace PlayHouse.Service.Play.Base.Command;

internal class StageTimerCmd : IBaseStageCmd
{
    private readonly LOG<StageTimerCmd> _log = new();

    public async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var timerCallback = routePacket.TimerCallback;
        var timerId = routePacket.TimerId;
        if (baseStage.HasTimer(timerId))
        {
            var task = timerCallback!.Invoke();
            await task.ConfigureAwait(false);
        }
        else
        {
            _log.Debug(() => $"timer already canceled - [stageId:{baseStage.StageId}, timerId:{timerId}]");
        }
    }
}