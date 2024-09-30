﻿using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Session.Network;
using PlayHouse.Service.Shared;

namespace PlayHouse.Service.Session;

internal class XSessionSender(
    ushort serviceId,
    IClientCommunicator clientCommunicator,
    RequestCache reqCache,
    ISession session,
    ISessionDispatcher sessionDispatcher)
    : XSender(serviceId, clientCommunicator, reqCache), ISessionSender
{
    private ushort _msgSeq;
    public void RelayToStage(string playEndpoint, long stageId, long sid, long accountId, ClientPacket packet)
    {
        var routePacket = RoutePacket.ApiOf(packet.ToRoutePacket(), false, false);
        routePacket.RouteHeader.StageId = stageId;
        routePacket.RouteHeader.AccountId = accountId;
        routePacket.RouteHeader.Header.MsgSeq = packet.MsgSeq;
        routePacket.RouteHeader.Sid = sid;
        routePacket.RouteHeader.IsToClient = false;
        ClientCommunicator.Send(playEndpoint, routePacket);
    }

    public void RelayToApi(string apiEndpoint, long sid, long accountId, ClientPacket packet)
    {
        var routePacket = RoutePacket.ApiOf(packet.ToRoutePacket(), false, false);
        routePacket.RouteHeader.Sid = sid;
        routePacket.RouteHeader.Header.MsgSeq = packet.MsgSeq;
        routePacket.RouteHeader.IsToClient = false;
        routePacket.RouteHeader.AccountId = accountId;

        ClientCommunicator.Send(apiEndpoint, routePacket);
    }

    public void SendToClient(IPacket packet)
    {
        var routePacket = RoutePacket.Of(packet);
        routePacket.Header.ServiceId = ServiceId;

        sessionDispatcher.SendToClient(session, routePacket.ToClientPacket());
    }

    public void ReplyToClient(IPacket packet)
    {
        var routePacket = RoutePacket.Of(packet);

        routePacket.Header.MsgSeq = _msgSeq;
        routePacket.Header.ServiceId = ServiceId;
        
        sessionDispatcher.SendToClient(session,routePacket.ToClientPacket());
        
    }

    public void SetClientRequestMsgSeq(ushort headerMsgSeq)
    {
        _msgSeq = headerMsgSeq;
    }
}