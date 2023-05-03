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
    public class XApiCommonSender : XSender, IApiCommonSender
    {
        private short serviceId;
        private IClientCommunicator clientCommunicator;
        private RequestCache reqCache;

        public XApiCommonSender(short serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
            : base(serviceId, clientCommunicator, reqCache)
        {
            this.serviceId = serviceId;
            this.clientCommunicator = clientCommunicator;
            this.reqCache = reqCache;
        }

        public long AccountId => _currentHeader?.AccountId ?? 0;

        public async Task<CreateStageResult> CreateStage(string playEndpoint, string stageType, long stageId, Packet packet)
        {
            var req = new CreateStageReq()
            {
                StageType = stageType,
                PayloadId = packet.MsgId,
                Payload = ByteString.CopyFrom(packet.Data)
            };

            var reply = await RequestToBaseStage(playEndpoint, stageId, 0, new Packet(req));

            var res = CreateStageRes.Parser.ParseFrom(reply.Data);

            return new CreateStageResult(reply.ErrorCode, new Packet(res.PayloadId, res.Payload));
        }

       
    }
}
