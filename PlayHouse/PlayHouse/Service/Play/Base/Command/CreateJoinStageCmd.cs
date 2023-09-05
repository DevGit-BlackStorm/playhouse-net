using Google.Protobuf;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Production;

namespace PlayHouse.Service.Play.Base.Command
{
    public class CreateJoinStageCmd : IBaseStageCmd
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
            var createStagePacket = new Packet(request.CreatePayloadId, request.CreatePayload);
            var stageType = request.StageType;
            var joinStagePacket = new Packet(request.JoinPayloadId, request.JoinPayload);
            var accountId = routePacket.AccountId;
            var stageId = routePacket.StageId;
            var sessionEndpoint = request.SessionEndpoint;
            var sid = request.Sid;
            var apiEndpoint = routePacket.RouteHeader.From;

            ReplyPacket createReply;
            CreateJoinStageRes response = new CreateJoinStageRes();

            if (!_playProcessor.IsValidType(stageType))
            {
                _playProcessor.ErrorReply(routePacket.RouteHeader,(int) BaseErrorCode.StageTypeIsInvalid);
                return;
            }

            if (!baseStage.IsCreated)
            {
                createReply = await baseStage.Create(stageType, createStagePacket);
                response.CreatePayloadId = createReply.MsgId;
                response.CreatePayload = ByteString.CopyFrom(createReply.Data);

                if (!createReply.IsSuccess())
                {
                    _playProcessor.RemoveRoom(stageId);                    
                    baseStage.Reply(new ReplyPacket(createReply.ErrorCode, response));
                    return;
                }
                else
                {
                    await baseStage.OnPostCreate();
                    response.IsCreated = true;
                }
            }

            var joinResult = await baseStage.Join(accountId, sessionEndpoint, sid, apiEndpoint, joinStagePacket);
            var joinReply = joinResult.Item1;
            var stageIndex = joinResult.Item2;

            response.JoinPayloadId = joinReply.MsgId;
            response.JoinPayload = ByteString.CopyFrom(joinReply.Data);
            response.StageIdx = stageIndex;


            baseStage.Reply(new ReplyPacket(joinReply.ErrorCode, response));

            if (joinReply.IsSuccess())
            {
                await baseStage.OnPostJoinRoom(accountId);
            }
        }
    }

}
