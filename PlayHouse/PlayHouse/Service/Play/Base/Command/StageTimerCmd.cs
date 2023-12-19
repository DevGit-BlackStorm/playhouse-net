using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Utils;


namespace PlayHouse.Service.Play.Base.Command;

internal class StageTimerCmd : IBaseStageCmd
{
    private readonly LOG<StageTimerCmd> _log = new ();
    public PlayProcessor PlayProcessor { get; }

    public StageTimerCmd(PlayProcessor playProcessor)
    {
        PlayProcessor = playProcessor;
    }

    public async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var timerCallback = routePacket.TimerCallback;
        var timerId = routePacket.TimerId;
        if (baseStage.HasTimer(timerId))
        {
           var task  = (Task)timerCallback!.Invoke();
           await task.ConfigureAwait(false);
        }
        else
        {
            _log.Debug(()=>$"timer already canceled - [stageId:{baseStage.StageId}, timerId:{timerId}]");
        }
    }
}

