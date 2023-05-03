using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production
{
    using Google.Protobuf;
    using Playhouse.Protocol;
    using PlayHouse.Communicator.Message;


    public delegate void ReplyCallback(ReplyPacket replyPacket);

    public interface IBasePacket : IDisposable
    {
        IPayload MovePayload();

        ReadOnlySpan<byte> Data { get; }
    }



    public class Packet : IBasePacket
    {
        public int MsgId;
        public IPayload Payload => _payload;

        private IPayload _payload;

        public Packet(int msgId = 0)
        {
            MsgId = msgId;
            _payload = new EmptyPayload();
        }

        public Packet(int msgId, IPayload payload) : this(msgId)
        {
            _payload = payload;
        }


        public Packet(IMessage message) : this(message.Descriptor.Index, new ProtoPayload(message)) { }
        public Packet(int msgId, ByteString message) : this(msgId, new ByteStringPayload(message)) { }



        public ReadOnlySpan<byte> Data => _payload!.Data;

        public IPayload MovePayload()
        {

            IPayload temp = _payload;
            _payload = new EmptyPayload();
            return temp;
        }

        public void Dispose()
        {
            _payload.Dispose();
        }
    }

    public class ReplyPacket : IBasePacket
    {
        public short ErrorCode { get; private set; }
        public int MsgId { get; private set; }
        public IPayload Payload => _payload;
        private IPayload _payload;

        public ReplyPacket(short errorCode, int msgId, IPayload payload)
        {
            ErrorCode = errorCode;
            MsgId = msgId;
            _payload = payload;
        }

        public ReplyPacket(short errorCode = 0, int msgId = 0) : this(errorCode, msgId, new EmptyPayload()) { }

        public ReplyPacket(short errorCode, IMessage message) : this(errorCode, message.Descriptor.Index, new ProtoPayload(message)) { }
        public ReplyPacket(IMessage message) : this(0, message.Descriptor.Index, new ProtoPayload(message)) { }


        public bool IsSuccess()
        {
            return ErrorCode == 0;
        }


        public ReadOnlySpan<byte> Data => _payload.Data;

        public IPayload MovePayload()
        {
            IPayload temp = _payload;
            _payload = new EmptyPayload();
            return temp;
        }

        public void Dispose()
        {
            _payload.Dispose();
        }
    }

}
