using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Play.Base.Command;
internal class CreateJoinStageCmd : IBaseStageCmd
{
    private readonly PlayDispatcher _dispatcher;

    public CreateJoinStageCmd(PlayDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
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

        if (!_dispatcher.IsValidType(stageType))
        {
            baseStage.Reply((int) BaseErrorCode.StageTypeIsInvalid);
            return;
        }

        if (!baseStage.IsCreated)
        {
            var createReply = await baseStage.Create(stageType, createStagePacket);
            response.CreatePayloadId = createReply.reply.MsgId;
            response.CreatePayload = ByteString.CopyFrom(createReply.reply.Payload.Data);

            if (createReply.errorCode == (ushort)BaseErrorCode.Success)
            {
                await baseStage.OnPostCreate();
                response.IsCreated = true;
            }
            else
            {
                _dispatcher.RemoveRoom(stageId);
                baseStage.Reply(createReply.errorCode);
                return;
            }
        }

        var joinResult = await baseStage.Join(accountId, sessionEndpoint, sid, apiEndpoint, joinStagePacket);

        response.JoinPayloadId = joinResult.reply.MsgId;
        response.JoinPayload = ByteString.CopyFrom(joinResult.reply.Payload.Data);
        response.StageIdx = joinResult.stageKey;


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

