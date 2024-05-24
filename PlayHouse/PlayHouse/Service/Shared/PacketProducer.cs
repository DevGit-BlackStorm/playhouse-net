using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared;

internal static class PacketProducer
{
    private static Func<int, IPayload, ushort, IPacket>? CreateFunc { get; set; } //msgId,


    public static void Init(Func<int, IPayload, ushort, IPacket> createFunc) //int msgId, payload,msgSeq return IPacket
    {
        PacketProducer.CreateFunc = createFunc;
    }

    public static IPacket CreatePacket(int msgId, IPayload payload, ushort msgSeq)
    {
        return CreateFunc!.Invoke(msgId, payload, msgSeq);
    }
}