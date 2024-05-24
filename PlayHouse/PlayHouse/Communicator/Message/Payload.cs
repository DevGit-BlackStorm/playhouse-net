using Google.Protobuf;
using NetMQ;

namespace PlayHouse.Communicator.Message;

public interface IPayload : IDisposable
{
    public ReadOnlyMemory<byte> Data { get; }
    public ReadOnlySpan<byte> DataSpan => Data.Span;
}

public class CopyPayload(IPayload payload) : IPayload
{
    private readonly byte[] _data = payload.Data.ToArray();

    public ReadOnlyMemory<byte> Data => _data;


    public void Dispose()
    {
    }
}

public class ProtoPayload(IMessage proto) : IPayload
{
    public ReadOnlyMemory<byte> Data => proto.ToByteArray();

    public void Dispose()
    {
    }

    public IMessage GetProto()
    {
        return proto;
    }
}

public class ByteStringPayload(ByteString byteString) : IPayload
{
    public void Dispose()
    {
    }

    public ReadOnlyMemory<byte> Data => byteString.ToByteArray();
}

public class EmptyPayload : IPayload
{
    public void Dispose()
    {
    }

    public ReadOnlyMemory<byte> Data => new();
}

public class FramePayload(NetMQFrame frame) : IPayload
{
    public NetMQFrame Frame { get; } = frame;

    public ReadOnlyMemory<byte> Data => new(Frame.Buffer, 0, Frame.MessageSize);

    public void Dispose()
    {
    }
}

public class PooledBytePayload(PooledByteBuffer ringBuffer) : IPayload
{
    public ReadOnlyMemory<byte> Data => ringBuffer.AsMemory();

    public void Dispose()
    {
        ringBuffer.Clear();
    }
}