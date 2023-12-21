using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;

namespace PlayHouse.Service.Play.Base.Command;
internal class CreateStageCmd : IBaseStageCmd
{
    private readonly PlayProcessor _playProcessor;
    public PlayProcessor PlayProcessor => _playProcessor;

    public CreateStageCmd(PlayProcessor playProcessor)
    {
        _playProcessor = playProcessor;
    }

    public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var createStageReq = CreateStageReq.Parser.ParseFrom(routePacket.Data);
        var packet = CPacket.Of(createStageReq.PayloadId, createStageReq.Payload);
        var stageType = createStageReq.StageType;

        if (!_playProcessor.IsValidType(stageType))
        {
            _playProcessor.ErrorReply(routePacket.RouteHeader, (ushort)BaseErrorCode.StageTypeIsInvalid);
            return;
        }

        var outcome = await baseStage.Create(stageType, packet);
        var stageId = baseStage.StageId;

        if (outcome.errorCode != (ushort)BaseErrorCode.Success)
        {
            this._playProcessor.RemoveRoom(stageId);
        }

        var res = new CreateStageRes()
        {
            Payload = ByteString.CopyFrom(outcome.reply.Payload.Data),
            PayloadId = outcome.reply.MsgId
        };

        
        baseStage.Reply(outcome.errorCode,CPacket.Of(res));

        if (outcome.errorCode == (ushort)BaseErrorCode.Success)
        {
            await baseStage.OnPostCreate();
        }
    }
}
