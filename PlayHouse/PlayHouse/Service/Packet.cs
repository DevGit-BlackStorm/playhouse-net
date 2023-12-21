namespace PlayHouse.Service;

using Google.Protobuf;
using Communicator.Message;
using System.Net.NetworkInformation;
using PlayHouse.Production;

public delegate void ReplyCallback(ushort errorCode, IPacket reply);

public interface IBasePacket : IDisposable
{
    IPayload MovePayload();
    ReadOnlySpan<byte> Data { get; }
}

internal class XPacket : IPacket
{
    private int _msgId;
    private IPayload _payload;

    private XPacket(int msgId, IPayload payload)
    {
        _msgId = msgId;
        _payload = payload;
    }


    public int MsgId => _msgId;

    public ReadOnlySpan<byte> Data => _payload.Data;
    public IPayload Payload => _payload;


    public static XPacket OfEmpty()
    {
        return new XPacket(0, new EmptyPayload());
    }

    public static XPacket Of(int msgId, ByteString message)
    {
        return new XPacket(msgId, new ByteStringPayload(message));
    }
    public static XPacket Of(IMessage message)
    {
        return new XPacket(message.Descriptor.Index, new ProtoPayload(message));
    }
    public static XPacket Of(int msgId, IPayload payload)
    {
        return new XPacket(msgId, payload);
    }

    public T Parse<T>()
    {
        throw new NotImplementedException();
    }

    internal static IPacket Of(IPacket packet)
    {
        return new XPacket(packet.MsgId, packet.Payload);
    }

    public IPacket Copy()
    {
        throw new NotImplementedException();
    }
}


internal class Packet : IBasePacket
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

    public static Packet Of(IPacket packet)
    {
        return new Packet(packet.MsgId, packet.Payload);
    }


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

    public IPacket ToContentsPacket()
    {
        return PacketProducer.Create!.Invoke(XPacket.Of(MsgId, _payload));
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

    public XPacket ToXPacket()
    {
        return XPacket.Of(MsgId, _payload);
    }
}
