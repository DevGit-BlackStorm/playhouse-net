
using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Play;
public class PacketHandler<TS, TA> where TA : IActor
{
    private readonly Dictionary<int, IPacketCmd<TS, TA>> _messageMap = new();

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

    public void Add(int msgId, IPacketCmd<TS, TA> cmd)
    {
        if (_messageMap.ContainsKey(msgId))
        {
            throw new ArgumentException($"msgId:{msgId} is already registered");
        }
        _messageMap[msgId] = cmd;
    }
}
