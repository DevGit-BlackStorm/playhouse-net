using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;

namespace PlayHouse.Service.Api;
internal class XApiCommonSender : XSender, IApiCommonSender
{
    protected XApiCommonSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
        : base(serviceId, clientCommunicator, reqCache)
    {
    }

    public string AccountId => CurrentHeader?.AccountId ?? string.Empty;

    public async Task<CreateStageResult> CreateStage(string playEndpoint, string stageType, string stageId, IPacket packet)
    {
        var req = new CreateStageReq()
        {
            StageType = stageType,
            PayloadId = packet.MsgId,
            Payload = ByteString.CopyFrom(packet.Payload.Data)
        };

        var reply = await RequestToBaseStage(playEndpoint, stageId, string.Empty, RoutePacket.Of(req));

        var res = CreateStageRes.Parser.ParseFrom(reply.Data);

        return new CreateStageResult(reply.ErrorCode,  PacketProducer.CreatePacket(res.PayloadId, new ByteStringPayload(res.Payload),packet.MsgSeq));
    }
}
