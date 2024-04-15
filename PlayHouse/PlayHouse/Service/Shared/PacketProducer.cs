using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared
{
    internal static class PacketProducer
    {

        private static Func<string, IPayload, ushort, IPacket>? _createFunc { get; set; } //msgId,


        public static void Init(Func<string, IPayload, ushort, IPacket> CreateFunc)//int msgId, payload,msgSeq return IPacket
        {
            _createFunc = CreateFunc;
        }

        public static IPacket CreatePacket(string msgId, IPayload payload, ushort msgSeq)
        {
            return _createFunc!.Invoke(msgId, payload, msgSeq);
        }
    }
}
