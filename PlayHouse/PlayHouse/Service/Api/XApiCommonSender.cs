using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Production;

namespace PlayHouse.Service.Api;
public class XApiCommonSender : XSender, IApiCommonSender
{
    private ushort serviceId;
    private IClientCommunicator clientCommunicator;
    private RequestCache reqCache;

    public XApiCommonSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
        : base(serviceId, clientCommunicator, reqCache)
    {
        this.serviceId = serviceId;
        this.clientCommunicator = clientCommunicator;
        this.reqCache = reqCache;
    }

    public Guid AccountId => CurrentHeader?.AccountId ?? Guid.Empty;

    public async Task<CreateStageResult> CreateStage(string playEndpoint, string stageType, Guid stageId, Packet packet)
    {
        var req = new CreateStageReq()
        {
            StageType = stageType,
            PayloadId = packet.MsgId,
            Payload = ByteString.CopyFrom(packet.Data)
        };

        var reply = await RequestToBaseStage(playEndpoint, stageId, Guid.Empty, new Packet(req));

        var res = CreateStageRes.Parser.ParseFrom(reply.Data);

        return new CreateStageResult(reply.ErrorCode, new Packet(res.PayloadId, res.Payload));
    }
}
