using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play.Base.Command;

internal class JoinStageCmd : IBaseStageCmd
{
    private PlayDispatcher _dispatcher;

    public JoinStageCmd(PlayDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var request = JoinStageReq.Parser.ParseFrom(routePacket.Span);
        var accountId = routePacket.AccountId;
        var sessionEndpoint = request.SessionEndpoint;
        var sid = request.Sid;
        var packet = CPacket.Of(request.PayloadId, request.Payload);
        var apiEndpoint = routePacket.RouteHeader.From;

        (ushort errorCode,IPacket reply,int stageKey) joinResult = await baseStage.Join(accountId, sessionEndpoint, sid, apiEndpoint, packet);

        var outcome = joinResult.reply;
        var stageIndex = joinResult.stageKey;
        var response = new JoinStageRes()
        {
            Payload = ByteString.CopyFrom(joinResult.reply.Payload.DataSpan),
            PayloadId = outcome.MsgId,
            StageIdx = stageIndex,
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