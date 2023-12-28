using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;

namespace PlayHouse.Service.Play.Base.Command;

internal class JoinStageCmd : IBaseStageCmd
{
    public PlayProcessor PlayProcessor { get; }

    public JoinStageCmd(PlayProcessor playProcessor)
    {
        PlayProcessor = playProcessor;
    }

    public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
    {
        var request = JoinStageReq.Parser.ParseFrom(routePacket.Data);
        var accountId = routePacket.AccountId;
        var sessionEndpoint = request.SessionEndpoint;
        var sid = request.Sid;
        var packet = CPacket.Of(request.PayloadId, request.Payload,routePacket.Header.MsgSeq);
        var apiEndpoint = routePacket.RouteHeader.From;

        (ReplyPacket reply, int stageKey) joinResult = await baseStage.Join(accountId, sessionEndpoint, sid, apiEndpoint, packet);

        var outcome = joinResult.reply;
        var stageIndex = joinResult.stageKey;
        var response = new JoinStageRes()
        {
            Payload = ByteString.CopyFrom(outcome.Data),
            PayloadId = outcome.MsgId,
            StageIdx = stageIndex,
        };

        baseStage.Reply(outcome.ErrorCode, CPacket.Of(response));

        if (outcome.IsSuccess())
        {
            await baseStage.OnPostJoinRoom(accountId);
        }
    }
}