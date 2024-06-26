﻿using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Play;

public class PacketHandler<TS, TA> where TA : IActor
{
    private readonly Dictionary<string, IPacketCmd<TS, TA>> _messageMap = new();

    public async Task Dispatch(TS stage, TA actor, IPacket packet)
    {
        if (_messageMap.TryGetValue(packet.MsgId, out var cmd))
        {
            await cmd.Execute(stage, actor, packet);
        }
        else
        {
            throw new ArgumentException($"msgId:{packet.MsgId} is not registered");
        }
    }

    public void Add(string msgId, IPacketCmd<TS, TA> cmd)
    {
        if (!_messageMap.TryAdd(msgId, cmd))
        {
            throw new ArgumentException($"msgId:{msgId} is already registered");
        }
    }
}