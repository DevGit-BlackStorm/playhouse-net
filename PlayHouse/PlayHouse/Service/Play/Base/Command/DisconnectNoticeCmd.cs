using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Play.Base.Command;

internal class DisconnectNoticeCmd : IBaseStageCmd
{
    public async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var accountId = routePacket.AccountId;
        await baseStage.OnDisconnect(accountId);
    }
}