using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;

namespace PlayHouse.Service.Play.Base.Command;
internal class CreateJoinStageCmd : IBaseStageCmd
{
    private readonly PlayProcessor _playProcessor;

    public PlayProcessor PlayProcessor => _playProcessor;

    public CreateJoinStageCmd(PlayProcessor playProcessor)
    {
        _playProcessor = playProcessor;
    }

    public async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var request = CreateJoinStageReq.Parser.ParseFrom(routePacket.Data);
        var createStagePacket = CPacket.Of(request.CreatePayloadId, request.CreatePayload);
        var stageType = request.StageType;
        var joinStagePacket = CPacket.Of(request.JoinPayloadId, request.JoinPayload);
        var accountId = routePacket.AccountId;
        var stageId = routePacket.StageId;
        var sessionEndpoint = request.SessionEndpoint;
        var sid = request.Sid;
        var apiEndpoint = routePacket.RouteHeader.From;

        CreateJoinStageRes response = new CreateJoinStageRes();

        if (!_playProcessor.IsValidType(stageType))
        {
            baseStage.Reply((int) BaseErrorCode.StageTypeIsInvalid);
            return;
        }

        if (!baseStage.IsCreated)
        {
            var createReply = await baseStage.Create(stageType, createStagePacket);
            response.CreatePayloadId = createReply.reply.MsgId;
            response.CreatePayload = ByteString.CopyFrom(createReply.reply.Payload.Data);

            if (createReply.errorCode != (ushort)BaseErrorCode.Success)
            {
                _playProcessor.RemoveRoom(stageId);                    
                baseStage.Reply(createReply.errorCode, CPacket.Of(response));
                return;
            }
            else
            {
                await baseStage.OnPostCreate();
                response.IsCreated = true;
            }
        }

        var joinResult = await baseStage.Join(accountId, sessionEndpoint, sid, apiEndpoint, joinStagePacket);
        var joinReply = joinResult.reply;
        var stageKey = joinResult.stageKey;

        response.JoinPayloadId = joinReply.MsgId;
        response.JoinPayload = ByteString.CopyFrom(joinReply.Data);
        response.StageIdx = stageKey;

        baseStage.Reply(joinReply.ErrorCode, CPacket.Of(response));

        if (joinReply.IsSuccess())
        {
            await baseStage.OnPostJoinRoom(accountId);
        }
    }
}

