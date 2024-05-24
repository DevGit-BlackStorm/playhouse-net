using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Play.Base.Command;

internal class AsyncBlockCmd : IBaseStageCmd
{
    public async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var asyncBlock = (AsyncBlockPacket)routePacket;
        await asyncBlock.AsyncPostCallback!.Invoke(asyncBlock.Result);
    }
}