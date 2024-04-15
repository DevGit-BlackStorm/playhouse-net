using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Play.Base.Command;
internal class DisconnectNoticeCmd : IBaseStageCmd
{
    public DisconnectNoticeCmd()
    {
    }

    public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        long accountId = routePacket.AccountId;
        await baseStage.OnDisconnect(accountId);
    }
}


