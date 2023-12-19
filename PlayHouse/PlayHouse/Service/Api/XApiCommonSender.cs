using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Production;

namespace PlayHouse.Service.Api;
public class XApiCommonSender : XSender, IApiCommonSender
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
            Payload = ByteString.CopyFrom(packet.Data)
        };

        var reply = await RequestToBaseStage(playEndpoint, stageId, string.Empty, new Packet(req));

        var res = CreateStageRes.Parser.ParseFrom(reply.Data);

        return new CreateStageResult(reply.ErrorCode, new Packet(res.PayloadId, res.Payload));
    }
}
