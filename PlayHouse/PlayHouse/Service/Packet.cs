namespace PlayHouse.Service;

using Google.Protobuf;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;

public delegate void ReplyCallback(ushort errorCode, IPacket reply);


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

internal class XPacket : IPacket
{
    private int _msgId;
    private IPayload _payload;

    private XPacket(int msgId, IPayload paylaod)
    {
        _msgId = msgId;
        _payload = paylaod;
    }
    public int MsgId => _msgId;

    public IPayload Payload => _payload;

    public static XPacket Of(IMessage message)
    {
        return new XPacket(message.Descriptor.Index, new ProtoPayload(message));
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

