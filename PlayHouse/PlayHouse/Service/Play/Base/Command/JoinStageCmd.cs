using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Production;

namespace PlayHouse.Service.Play.Base.Command
{
    public class JoinStageCmd : IBaseStageCmd
    {
        private readonly PlayProcessor _playProcessor;

        public PlayProcessor PlayProcessor => _playProcessor;

        public JoinStageCmd(PlayProcessor playProcessor)
        {
            _playProcessor = playProcessor;
        }

        public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
        {
            var request = JoinStageReq.Parser.ParseFrom(routePacket.Data);
            var accountId = routePacket.AccountId;
            var sessionEndpoint = request.SessionEndpoint;
            var sid = request.Sid;
            var packet = new Packet(request.PayloadId, request.Payload);
            var apiEndpoint = routePacket.RouteHeader.From;

            var joinResult = await baseStage.Join(accountId, sessionEndpoint, sid, apiEndpoint, packet);

            var outcome = joinResult.Item1;
            var stageIndex = joinResult.Item2;
            var response = new JoinStageRes()
            {
                Payload = ByteString.CopyFrom(outcome.Data),
                PayloadId = request.PayloadId, 
                StageIdx = stageIndex,
            };

            baseStage.Reply(new ReplyPacket(outcome.ErrorCode, response));

            if (outcome.IsSuccess())
            {
                await baseStage.OnPostJoinRoom(accountId);
            }
        }
    }

}
