using Google.Protobuf;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Api;

internal class AllApiSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
    : XApiCommonSender(serviceId, clientCommunicator, reqCache), IApiSender, IApiBackendSender
{
    public string GetFromEndpoint()
    {
        return CurrentHeader?.From ?? "";
    }

    public string SessionEndpoint => CurrentHeader?.From ?? "";
    public int Sid => CurrentHeader?.Sid ?? 0;

    public void Authenticate(long accountId)
    {
        var message = new AuthenticateMsg
        {
            ServiceId = ServiceId,
            AccountId = accountId
        };

        if (CurrentHeader != null)
        {
            SendToBaseSession(CurrentHeader.From, CurrentHeader.Sid, RoutePacket.Of(message));
        }
        else
        {
            throw new Exception(
                "request header is not exist ,This function should only be called from the request packet handle");
        }
    }


    public async Task<JoinStageResult> JoinStage(string playEndpoint, long stageId, IPacket packet)
    {
        var req = new JoinStageReq
        {
            SessionEndpoint = SessionEndpoint,
            Sid = Sid,
            PayloadId = packet.MsgId,
            Payload = ByteString.CopyFrom(packet.Payload.DataSpan)
        };

        using var reply = await RequestToBaseStage(playEndpoint, stageId, AccountId, RoutePacket.Of(req));

        var res = JoinStageRes.Parser.ParseFrom(reply.Span);

        return new JoinStageResult(reply.ErrorCode, CPacket.Of(res.PayloadId, res.Payload));
    }

    public async Task<CreateJoinStageResult> CreateJoinStage(string playEndpoint,
        string stageType,
        long stageId,
        IPacket createPacket,
        IPacket joinPacket)
    {
        var req = new CreateJoinStageReq
        {
            StageType = stageType,
            CreatePayloadId = createPacket.MsgId,
            CreatePayload = ByteString.CopyFrom(createPacket.Payload.DataSpan),
            SessionEndpoint = SessionEndpoint,
            Sid = Sid,
            JoinPayloadId = joinPacket.MsgId,
            JoinPayload = ByteString.CopyFrom(joinPacket.Payload.DataSpan)
        };

        using var reply = await RequestToBaseStage(playEndpoint, stageId, AccountId, RoutePacket.Of(req));

        var res = CreateJoinStageRes.Parser.ParseFrom(reply.Span);

        return new CreateJoinStageResult(
            reply.ErrorCode,
            res.IsCreated,
            CPacket.Of(res.CreatePayloadId, res.CreatePayload),
            CPacket.Of(res.JoinPayloadId, res.JoinPayload)
        );
    }
}