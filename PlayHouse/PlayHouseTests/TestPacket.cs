using Google.Protobuf;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;

namespace PlayHouseTests;

internal class TestPacket : IPacket
{
    private readonly ushort _msgSeq;

    public TestPacket(IMessage message)
    {
        MsgId = message.Descriptor.Name;
        Payload = new ProtoPayload(message);
    }

    public TestPacket(string msgId)
    {
        MsgId = msgId;
        Payload = new EmptyPayload();
    }

    public TestPacket(string msgId, IPayload payload, ushort msgSeq) : this(msgId)
    {
        Payload = payload;
        _msgSeq = msgSeq;
    }

    public bool IsRequest => _msgSeq > 0;

    public string MsgId { get; }

    public IPayload Payload { get; }

    public void Dispose()
    {
        Payload.Dispose();
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