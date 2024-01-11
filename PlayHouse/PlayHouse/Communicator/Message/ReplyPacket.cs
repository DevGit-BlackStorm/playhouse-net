using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.Message;


//internal class ReplyPacket : IBasePacket
//{
//    public ushort ErrorCode { get; private set; }
//    public int MsgId { get; private set; }
//    public IPayload Payload => _payload;
//    private IPayload _payload;

//    public ReplyPacket(ushort errorCode, int msgId, IPayload payload)
//    {
//        ErrorCode = errorCode;
//        MsgId = msgId;
//        _payload = payload;
//    }

//    public ReplyPacket(ushort errorCode = 0, int msgId = 0) : this(errorCode, msgId, new EmptyPayload()) { }

//    public ReplyPacket(ushort errorCode, IMessage message) : this(errorCode, message.Descriptor.Index, new ProtoPayload(message)) { }
//    public ReplyPacket(IMessage message) : this(0, message.Descriptor.Index, new ProtoPayload(message)) { }


//    public bool IsSuccess()
//    {
//        return ErrorCode == 0;
//    }


//    public ReadOnlySpan<byte> Data => _payload.Data;

//    public IPayload MovePayload()
//    {
//        IPayload temp = _payload;
//        _payload = new EmptyPayload();
//        return temp;
//    }

//    public void Dispose()
//    {
//        _payload.Dispose();
//    }

//}
