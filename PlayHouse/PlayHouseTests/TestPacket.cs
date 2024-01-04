using Google.Protobuf;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;

namespace PlayHouseTests
{
    internal class TestPacket : IPacket
    {
        private int _msgId;
        private IPayload _payload;
        private bool _isReqeust;

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

        public TestPacket(int msgId, IPayload payload, bool isReqeust) : this(msgId)
        {
            _payload = payload;
            _isReqeust = isReqeust;
        }

        public int MsgId => _msgId;

        public IPayload Payload => _payload;
        public bool IsRequest => _isReqeust;

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
