using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.Message
{
    using Google.Protobuf;
    using Playhouse.Protocol;

    //public interface IReplyCallback
    //{
    //    void OnReceive(ReplyPacket replyPacket);
    //}

    public delegate void ReplyCallback(ReplyPacket replyPacket);

    public interface IBasePacket : IDisposable
    {
        IPayload MovePayload();
        
        (byte[],int) Data();
    }




    public class Packet : IBasePacket
    {
        public short MsgId;
        private IPayload _payload;

        public Packet(short msgId = -1)
        {
            this.MsgId = msgId;
            this._payload =new EmptyPayload();
        }

        public Packet(short msgId, IPayload payload) : this(msgId)
        {
            _payload = payload;
        }
          

        public Packet(IMessage message) : this((short)message.Descriptor.Index, new ProtoPayload(message)) { }
        public Packet(short msgId, ByteString message) : this(msgId, new ByteStringPayload(message)) { }

        

        public (byte[],int) Data()
        {
            return (_payload!.Data());
        }

        public IPayload MovePayload()
        {
            
            IPayload temp = _payload;
            _payload = new EmptyPayload() ;
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
        public short MsgId { get; private set; }
        private IPayload _payload;

        public ReplyPacket(short errorCode, short msgId, IPayload payload)
        {
            this.ErrorCode = errorCode;
            this.MsgId = msgId;
            this._payload = payload;
        }

        public ReplyPacket(short errorCode = 0, short msgId = -1):this(errorCode,msgId,new EmptyPayload()){}

        public ReplyPacket(short errorCode, IMessage message) : this(errorCode, (short)message.Descriptor.Index, new ProtoPayload(message)) { }
        public ReplyPacket(IMessage message) : this(0, (short)message.Descriptor.Index, new ProtoPayload(message)) { }
        

        public bool IsSuccess()
        {
            return ErrorCode == 0;
        }

   
        public (byte[], int)  Data()
        {
            return _payload.Data();
        }

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
