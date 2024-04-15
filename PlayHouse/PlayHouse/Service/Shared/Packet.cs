namespace PlayHouse.Service.Shared;

using Google.Protobuf;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;

public delegate void ReplyCallback(ushort errorCode, IPacket reply);


internal class CPacket
{

    public static IPacket Of(string msgId, ByteString message)
    {
        return PacketProducer.CreatePacket(msgId, new ByteStringPayload(message), 0);
    }
    public static IPacket Of(IMessage message)
    {
        return PacketProducer.CreatePacket(message.Descriptor.Name, new ProtoPayload(message), 0);
    }
    public static IPacket Of(string msgId, IPayload payload)
    {
        return PacketProducer.CreatePacket(msgId, payload, 0);
    }

    //public static IPacket Of(ReplyPacket replyPacket)
    //{
    //    return PacketProducer.CreatePacket(replyPacket.MsgId, replyPacket.Payload, 0);
    //}

    public static IPacket Of(RoutePacket packet)
    {
        return PacketProducer.CreatePacket(packet.MsgId, packet.Payload, 0);
    }

}

internal class XPacket : IPacket
{
    private string _msgId;
    private IPayload _payload;
    private int _msgSeq;

    private XPacket(string msgId, IPayload paylaod, int msgSeq)
    {
        _msgId = msgId;
        _payload = paylaod;
        _msgSeq = msgSeq;
    }
    public string MsgId => _msgId;

    public IPayload Payload => _payload;
    public int MsgSeq { get => _msgSeq; set => _msgSeq = value; }

    public static XPacket Of(IMessage message)
    {
        return new XPacket(message.Descriptor.Name, new ProtoPayload(message), 0);
    }

    public void Dispose()
    {
        _payload.Dispose();
    }

    //public IPacket Copy()
    //{
    //    throw new NotImplementedException();
    //}
    //public T Parse<T>()
    //{
    //    throw new NotImplementedException();
    //}

}

internal class EmptyPacket : IPacket
{
    private IPayload _payload = new EmptyPayload();
    public string MsgId => string.Empty;
    private int _msgSeq;
    public int MsgSeq { get => 0; set => _msgSeq = value; }
    public IPayload Payload => _payload;

    public void Dispose()
    {
        _payload.Dispose();
    }

    //public IPacket Copy()
    //{
    //    throw new NotImplementedException();
    //}

    //public T Parse<T>()
    //{
    //    throw new NotImplementedException();
    //}

}

