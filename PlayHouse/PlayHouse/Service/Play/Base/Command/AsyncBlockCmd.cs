using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Play.Base.Command;
public class AsyncBlockCmd : IBaseStageCmd
{
    public PlayProcessor PlayProcessor { get; }

    public AsyncBlockCmd(PlayProcessor playProcessor)
    {
        PlayProcessor = playProcessor;
    }

    public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        AsyncBlockPacket asyncBlock = (AsyncBlockPacket)routePacket;
        await asyncBlock.AsyncPostCallback!.Invoke(asyncBlock.Result);
    }
}

