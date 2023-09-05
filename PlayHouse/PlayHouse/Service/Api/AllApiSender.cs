using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Api
{
    public class AllApiSender : XApiCommonSender, IApiSender, IApiBackendSender
    {
        private ushort serviceId;
        private IClientCommunicator clientCommunicator;
        private RequestCache reqCache;

        public AllApiSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
            : base(serviceId, clientCommunicator, reqCache)
        {
            this.serviceId = serviceId;
            this.clientCommunicator = clientCommunicator;
            this.reqCache = reqCache;
        }

        public string GetFromEndpoint()
        {
            return _currentHeader?.From ?? "";
        }

        public string SessionEndpoint => _currentHeader?.From ?? "";
        public int Sid => _currentHeader?.Sid ?? 0;

        public void Authenticate(Guid accountId)
        {
            var message = new AuthenticateMsg()
            {
                ServiceId = (int)serviceId,
                AccountId = ByteString.CopyFrom(accountId.ToByteArray())
            };

            if (_currentHeader != null)
            {
                SendToBaseSession(_currentHeader.From, _currentHeader.Sid, new Packet(message));
            }
            else
            {
                throw new ApiException.NotExistApiHeaderInfoException();
            }
        }

  

        public async Task<JoinStageResult> JoinStage(string playEndpoint, Guid stageId, Packet packet)
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
                                                Guid stageId,
                                                Packet createPacket,                
                                                Packet joinPacket)
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
