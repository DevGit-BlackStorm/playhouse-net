using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Play.Base.Command;

internal class JoinStageCmd(PlayDispatcher dispatcher) : IBaseStageCmd
{
    private PlayDispatcher _dispatcher = dispatcher;

    public async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var request = JoinStageReq.Parser.ParseFrom(routePacket.Span);
        var accountId = routePacket.AccountId;
        var sessionEndpoint = request.SessionEndpoint;
        var sid = request.Sid;
        var packet = CPacket.Of(request.PayloadId, request.Payload);
        var apiEndpoint = routePacket.RouteHeader.From;

        var joinResult = await baseStage.Join(accountId, sessionEndpoint, sid, apiEndpoint, packet);

        var outcome = joinResult.reply;
        var response = new JoinStageRes
        {
            Payload = ByteString.CopyFrom(joinResult.reply.Payload.DataSpan),
            PayloadId = outcome.MsgId
        };

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