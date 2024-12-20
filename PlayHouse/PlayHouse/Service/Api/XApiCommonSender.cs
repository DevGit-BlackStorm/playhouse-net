﻿using Google.Protobuf;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Api;

internal class XApiCommonSender : XSender, IApiCommonSender
{
    protected XApiCommonSender(ushort serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
        : base(serviceId, clientCommunicator, reqCache)
    {
    }

    public long AccountId => CurrentHeader?.AccountId ?? 0;

    public async Task<CreateStageResult> CreateStage(string playNid, string stageType, long stageId,
        IPacket packet)
    {
        var req = new CreateStageReq
        {
            StageType = stageType,
            PayloadId = packet.MsgId,
            Payload = ByteString.CopyFrom(packet.Payload.DataSpan)
        };

        using var reply = await RequestToBaseStage(playNid, stageId, 0, RoutePacket.Of(req));

        var res = CreateStageRes.Parser.ParseFrom(reply.Span);

        return new CreateStageResult(reply.ErrorCode, CPacket.Of(res.PayloadId, new ByteStringPayload(res.Payload)));
    }
}