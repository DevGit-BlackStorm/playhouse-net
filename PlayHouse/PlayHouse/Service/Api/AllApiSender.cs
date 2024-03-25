using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Api
{
    internal class AllApiSender : XApiCommonSender, IApiSender, IApiBackendSender
    {
        private readonly ushort _serviceId;

        public AllApiSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
            : base(serviceId, clientCommunicator, reqCache)
        {
            this._serviceId = serviceId;
        }

        public string GetFromEndpoint()
        {
            return CurrentHeader?.From ?? "";
        }

        public string SessionEndpoint => CurrentHeader?.From ?? "";
        public int Sid => CurrentHeader?.Sid ?? 0;

        public void Authenticate(string accountId)
        {
            if(accountId == null || accountId == string.Empty) 
            {
                throw new InvalidDataException("accountId is null or empty");
            }
            var message = new AuthenticateMsg()
            {
                ServiceId = (int)_serviceId,
                AccountId = accountId
            };

            if (CurrentHeader != null)
            {
                SendToBaseSession(CurrentHeader.From, CurrentHeader.Sid, RoutePacket.Of(message));
            }
            else
            {
                throw new Exception("request header is not exist ,This function should only be called from the request packet handle");
            }
        }

  

        public async Task<JoinStageResult> JoinStage(string playEndpoint, string stageId, IPacket packet)
        {
            var req = new JoinStageReq()
            {
                SessionEndpoint = this.SessionEndpoint,
                Sid = this.Sid,
                PayloadId = packet.MsgId,
                Payload = ByteString.CopyFrom(packet.Payload.DataSpan),
            };

            using RoutePacket reply = await RequestToBaseStage(playEndpoint, stageId, this.AccountId, RoutePacket.Of(req));

            JoinStageRes res = JoinStageRes.Parser.ParseFrom(reply.Span);

            return new JoinStageResult(reply.ErrorCode, res.StageIdx, CPacket.Of(res.PayloadId, res.Payload));
        }

        public async Task<CreateJoinStageResult> CreateJoinStage(string playEndpoint,
                                                string stageType,
                                                string stageId,
                                                IPacket createPacket,                
                                                IPacket joinPacket)
        {
            var req = new CreateJoinStageReq()
            {
                StageType = stageType,
                CreatePayloadId = createPacket.MsgId,
                CreatePayload = ByteString.CopyFrom(createPacket.Payload.DataSpan),
                SessionEndpoint = this.SessionEndpoint,
                Sid = this.Sid,
                JoinPayloadId = joinPacket.MsgId,
                JoinPayload = ByteString.CopyFrom(joinPacket.Payload.DataSpan),
            };

            using var reply = await RequestToBaseStage(playEndpoint, stageId, this.AccountId, RoutePacket.Of(req));

            var res = CreateJoinStageRes.Parser.ParseFrom(reply.Span);

            return new CreateJoinStageResult(
                    reply.ErrorCode,
                    res.IsCreated,
                    res.StageIdx,
                    CPacket.Of(res.CreatePayloadId, res.CreatePayload),
                    CPacket.Of(res.JoinPayloadId, res.JoinPayload)
            );
        }
    }
}
