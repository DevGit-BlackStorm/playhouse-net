using Google.Protobuf;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;

namespace PlayHouseTests
{
    internal class TestPacket : IPacket
    {
        private string _msgId;
        private IPayload _payload;
        private ushort _msgSeq;

        public TestPacket(IMessage message)
        {
            _msgId = message.Descriptor.Name;
            _payload = new ProtoPayload(message);
        }

        public TestPacket(string msgId)
        {
            _msgId = msgId;
            _payload = new EmptyPayload();
        }

        public TestPacket(string msgId, IPayload payload, ushort msgSeq) : this(msgId)
        {
            _payload = payload;
            _msgSeq = msgSeq;
        }

        public string MsgId => _msgId;

        public IPayload Payload => _payload;
        public bool IsRequest => _msgSeq > 0;

        public IPacket Copy()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _payload.Dispose();
        }

        public T Parse<T>()
        {
            throw new NotImplementedException();
        }
    }
}
