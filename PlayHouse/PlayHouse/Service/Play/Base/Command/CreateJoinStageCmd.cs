using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Play.Base.Command;

internal class CreateJoinStageCmd(PlayDispatcher dispatcher) : IBaseStageCmd
{
    public async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var request = CreateJoinStageReq.Parser.ParseFrom(routePacket.Span);
        var createStagePacket = CPacket.Of(request.CreatePayloadId, request.CreatePayload);
        var stageType = request.StageType;
        var joinStagePacket = CPacket.Of(request.JoinPayloadId, request.JoinPayload);
        var accountId = routePacket.AccountId;
        var stageId = routePacket.StageId;
        var sessionEndpoint = request.SessionNid;
        var sid = request.Sid;
        var apiEndpoint = routePacket.RouteHeader.From;

        var response = new CreateJoinStageRes();

        if (!dispatcher.IsValidType(stageType))
        {
            baseStage.Reply((int)BaseErrorCode.StageTypeIsInvalid);
            return;
        }

        if (!baseStage.IsCreated)
        {
            var createReply = await baseStage.Create(stageType, createStagePacket);
            response.CreatePayloadId = createReply.reply.MsgId;
            response.CreatePayload = ByteString.CopyFrom(createReply.reply.Payload.DataSpan);

            if (createReply.errorCode == (ushort)BaseErrorCode.Success)
            {
                await baseStage.OnPostCreate();
                response.IsCreated = true;
            }
            else
            {
                dispatcher.RemoveRoom(stageId);
                baseStage.Reply(createReply.errorCode);
                return;
            }
        }

        var joinResult = await baseStage.Join(accountId, sessionEndpoint, sid, apiEndpoint, joinStagePacket);

        response.JoinPayloadId = joinResult.reply.MsgId;
        response.JoinPayload = ByteString.CopyFrom(joinResult.reply.Payload.DataSpan);

        if (joinResult.errorCode == (ushort)BaseErrorCode.Success)
        {
            baseStage.Reply(CPacket.Of(response));
            await baseStage.OnPostJoinRoom(accountId);
        }
        else
        {
            baseStage.Reply(joinResult.errorCode);
        }
    }
}