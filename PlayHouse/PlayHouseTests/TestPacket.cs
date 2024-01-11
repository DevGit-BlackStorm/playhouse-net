using Google.Protobuf;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;

namespace PlayHouseTests
{
    internal class TestPacket : IPacket
    {
        private int _msgId;
        private IPayload _payload;
        private ushort _msgSeq;

        public TestPacket(IMessage message)
        {
            _msgId = message.Descriptor.Index;
            _payload = new ProtoPayload(message);
        }

        public TestPacket(int msgId)
        {
            _msgId = msgId;
            _payload = new EmptyPayload();
        }

        public TestPacket(int msgId, IPayload payload, ushort msgSeq) : this(msgId)
        {
            _payload = payload;
            _msgSeq = msgSeq;
        }

        public int MsgId => _msgId;

        public IPayload Payload => _payload;
        public bool IsRequest => _msgSeq > 0;

        public IPacket Copy()
        {
            throw new NotImplementedException();
        }

        public T Parse<T>()
        {
            throw new NotImplementedException();
        }
    }
}
