using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Play.Base.Command;
internal class AsyncBlockCmd : IBaseStageCmd
{

    public AsyncBlockCmd()
    {
    }

    public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        AsyncBlockPacket asyncBlock = (AsyncBlockPacket)routePacket;
        await asyncBlock.AsyncPostCallback!.Invoke(asyncBlock.Result);
    }
}

