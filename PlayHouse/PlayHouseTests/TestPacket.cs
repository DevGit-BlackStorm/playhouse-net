using Google.Protobuf;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouseTests
{
    internal class TestPacket : IPacket
    {
        private int _msgId;
        private IPayload _payload;
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

        public TestPacket(int msgId, IPayload payload) : this(msgId)
        {
            _payload = payload;
        }

        public int MsgId => _msgId;

        public IPayload Payload => _payload;

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
