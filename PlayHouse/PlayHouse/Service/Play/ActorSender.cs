﻿using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play;

public interface IActorSender
{
    long AccountId();
    string SessionEndpoint();
    string ApiEndpoint();
    long Sid();
    void LeaveStage();

    void SendToClient(IPacket packet);

    void SendToApi(IPacket packet);
    Task<IPacket> RequestToApi(IPacket packet);
    Task<IPacket> AsyncToApi(IPacket packet);
}