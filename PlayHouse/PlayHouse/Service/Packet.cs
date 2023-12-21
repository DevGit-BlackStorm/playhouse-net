namespace PlayHouse.Service;

using Google.Protobuf;
using Communicator.Message;
using System.Net.NetworkInformation;
using PlayHouse.Production;
using NetMQ;

public delegate void ReplyCallback(ushort errorCode, IPacket reply);

public interface IBasePacket : IDisposable
{
    IPayload MovePayload();
    ReadOnlySpan<byte> Data { get; }
}

internal class CPacket
{

    public static IPacket Of(int msgId, ByteString message)
    {
        return PacketProducer.CreatePacket(msgId, new ByteStringPayload(message));
    }
    public static IPacket Of(IMessage message)
    {
        return PacketProducer.CreatePacket(message.Descriptor.Index, new ProtoPayload(message));
    }
    public static IPacket Of(int msgId, IPayload payload)
    {
        return PacketProducer.CreatePacket(msgId, payload);
    }

    public static IPacket Of(ReplyPacket replyPacket)
    {
        return PacketProducer.CreatePacket(replyPacket.MsgId, replyPacket.Payload);
    }

    public static IPacket OfEmpty()
    {
        return new EmptyPacket();
    }
}


//internal class Packet 
//{
//    public int MsgId;
//    public IPayload Payload => _payload;

//    private IPayload _payload;

//    public Packet(int msgId = 0)
//    {
//        MsgId = msgId;
//        _payload = new EmptyPayload();
//    }

//    public Packet(int msgId, IPayload payload) : this(msgId)
//    {
//        _payload = payload;
//    }

//    public Packet(IMessage message) : this(message.Descriptor.Index, new ProtoPayload(message)) { }
//    public Packet(int msgId, ByteString message) : this(msgId, new ByteStringPayload(message)) { }

//    public static Packet Of(IPacket packet)
//    {
//        return new Packet(packet.MsgId, packet.Payload);
//    }


//    public ReadOnlySpan<byte> Data => _payload!.Data;

//    //public IPayload MovePayload()
//    //{

//    //    IPayload temp = _payload;
//    //    _payload = new EmptyPayload();
//    //    return temp;
//    //}

//    //public void Dispose()
//    //{
//    //    _payload.Dispose();
//    //}

//    public IPacket ToContentsPacket()
//    {
//        return PacketProducer.CreatePacket(MsgId, _payload);
//    }
//}

internal class XPacket : IPacket
{
    private int _msgId;
    private IPayload _payload;

    private XPacket(int msgId,IPayload paylaod)
    {
        _msgId = msgId;
        _payload = paylaod;
    }
    public int MsgId => _msgId;

    public IPayload Payload => _payload;


    public static XPacket Of(IMessage message)
    {
        return new XPacket(message.Descriptor.Index,new ProtoPayload(message));
    }

    public IPacket Copy()
    {
        throw new NotImplementedException();
    }

    public T Parse<T>()
    {
        throw new NotImplementedException();
    }
}

internal class EmptyPacket : IPacket
{
    private IPayload _payload = new EmptyPayload();
    public int MsgId => 0;

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

internal class ReplyPacket : IBasePacket
{
    public ushort ErrorCode { get; private set; }
    public int MsgId { get; private set; }
    public IPayload Payload => _payload;
    private IPayload _payload;

    public ReplyPacket(ushort errorCode, int msgId, IPayload payload)
    {
        ErrorCode = errorCode;
        MsgId = msgId;
        _payload = payload;
    }

    public ReplyPacket(ushort errorCode = 0, int msgId = 0) : this(errorCode, msgId, new EmptyPayload()) { }

    public ReplyPacket(ushort errorCode, IMessage message) : this(errorCode, message.Descriptor.Index, new ProtoPayload(message)) { }
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
