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
        
        byte[] Data();
    }




    public class Packet : IBasePacket
    {
        public string MsgName;
        private IPayload _payload;

        public Packet(string msgName = "")
        {
            this.MsgName = msgName;
            this._payload =new EmptyPayload();
        }

        public Packet(string msgName, IPayload payload) : this(msgName)
        {
            _payload = payload;
        }
          

        public Packet(IMessage message) : this(message.Descriptor.Name, new ProtoPayload(message)) { }
        public Packet(string msgName, ByteString message) : this(msgName, new ByteStringPayload(message)) { }

        

        public byte[] Data()
        {
            return _payload!.Data();
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
        public int ErrorCode { get; private set; }
        public string MsgName { get; private set; }
        private IPayload _payload;

        public ReplyPacket(int errorCode, string msgName, IPayload payload)
        {
            this.ErrorCode = errorCode;
            this.MsgName = msgName;
            this._payload = payload;
        }

        public ReplyPacket(int errorCode = 0, string msgName = ""):this(errorCode,msgName,new EmptyPayload()){}

        public ReplyPacket(int errorCode, IMessage message) : this(errorCode, message.Descriptor.Name, new ProtoPayload(message)) { }
        public ReplyPacket(IMessage message) : this(0, message.Descriptor.Name, new ProtoPayload(message)) { }
        

        public bool IsSuccess()
        {
            return ErrorCode == 0;
        }

   
        public byte[] Data()
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
