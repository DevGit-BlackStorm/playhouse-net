﻿namespace PlayHouse.Production.Shared;

public interface ISystemPanel
{
    IServerInfo GetServerInfo();
    IServerInfo GetServerInfoBy(ushort serviceId);
    IServerInfo GetServerInfoBy(ushort serviceId, long accountId);
    IServerInfo GetServerInfoByNid(int nid);
    IList<IServerInfo> GetServers();
    void Pause();
    void Resume();
    Task ShutdownASync();
    ServerState GetServerState();
    long GenerateUUID();
}