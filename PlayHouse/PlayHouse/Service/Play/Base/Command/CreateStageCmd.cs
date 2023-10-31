using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Production;

namespace PlayHouse.Service.Play.Base.Command;
public class CreateStageCmd : IBaseStageCmd
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
        var packet = new Packet(createStageReq.PayloadId, createStageReq.Payload);
        var stageType = createStageReq.StageType;

        if (!_playProcessor.IsValidType(stageType))
        {
            _playProcessor.ErrorReply(routePacket.RouteHeader, (ushort)BaseErrorCode.StageTypeIsInvalid);
            return;
        }

        var outcome = await baseStage.Create(stageType, packet);
        var stageId = baseStage.StageId;

        if (!outcome.IsSuccess())
        {
            this._playProcessor.RemoveRoom(stageId);
        }

        var res = new CreateStageRes()
        {
            Payload = ByteString.CopyFrom(outcome.Data),
            PayloadId = outcome.MsgId
        };

        
        baseStage.Reply(new ReplyPacket(outcome.ErrorCode, res));

        if (outcome.IsSuccess())
        {
            await baseStage.OnPostCreate();
        }
    }
}
