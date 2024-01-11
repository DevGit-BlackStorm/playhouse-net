using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Play.Base.Command;
internal class CreateStageCmd : IBaseStageCmd
{
    private readonly PlayDispatcher _dispatcher;

    public CreateStageCmd(PlayDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var createStageReq = CreateStageReq.Parser.ParseFrom(routePacket.Data);
        var packet = CPacket.Of(createStageReq.PayloadId, createStageReq.Payload);
        var stageType = createStageReq.StageType;

        if (!_dispatcher.IsValidType(stageType))
        {
            baseStage.Reply((ushort)BaseErrorCode.StageTypeIsInvalid);
            return;
        }

        var outcome = await baseStage.Create(stageType, packet);
        var stageId = baseStage.StageId;

        if (outcome.errorCode == (ushort)BaseErrorCode.Success)
        {
            var res = new CreateStageRes()
            {
                Payload = ByteString.CopyFrom(outcome.reply.Payload.Data),
                PayloadId = outcome.reply.MsgId
            };

            baseStage.Reply(CPacket.Of(res));
            await baseStage.OnPostCreate();
        }
        else
        {

            this._dispatcher.RemoveRoom(stageId);
            baseStage.Reply(outcome.errorCode);
        }
    }
}
