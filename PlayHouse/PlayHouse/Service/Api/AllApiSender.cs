using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Production;

namespace PlayHouse.Service.Api
{
    public class AllApiSender : XApiCommonSender, IApiSender, IApiBackendSender
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
                SendToBaseSession(CurrentHeader.From, CurrentHeader.Sid, new Packet(message));
            }
            else
            {
                throw new ApiException.NotExistApiHeaderInfoException();
            }
        }

  

        public async Task<JoinStageResult> JoinStage(string playEndpoint, string stageId, IPacket packet)
        {
            var req = new JoinStageReq()
            {
                SessionEndpoint = this.SessionEndpoint,
                Sid = this.Sid,
                PayloadId = packet.MsgId,
                Payload = ByteString.CopyFrom(packet.Data),
            };

            var reply = await RequestToBaseStage(playEndpoint, stageId, this.AccountId, new Packet(req));

            var res = JoinStageRes.Parser.ParseFrom(reply.Data);

            return new JoinStageResult(reply.ErrorCode, res.StageIdx, new Packet(res.PayloadId, res.Payload));
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
                CreatePayload = ByteString.CopyFrom(createPacket.Data),
                SessionEndpoint = this.SessionEndpoint,
                Sid = this.Sid,
                JoinPayloadId = joinPacket.MsgId,
                JoinPayload = ByteString.CopyFrom(joinPacket.Data),
            };

            var reply = await RequestToBaseStage(playEndpoint, stageId, this.AccountId, new Packet(req));

            var res = CreateJoinStageRes.Parser.ParseFrom(reply.Data);

            return new CreateJoinStageResult(
                    reply.ErrorCode,
                    res.IsCreated,
                    res.StageIdx,
                    new Packet(res.CreatePayloadId, res.CreatePayload),
                    new Packet(res.JoinPayloadId, res.JoinPayload)
            );
        }
    }
}
