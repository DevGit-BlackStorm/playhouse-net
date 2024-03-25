
using CommonLib;
using Google.Protobuf;
using NetMQ;

namespace PlayHouse.Communicator.Message
{
    public interface IPayload : IDisposable
    {
        public ReadOnlyMemory<byte> Data { get; }
        public ReadOnlySpan<byte> DataSpan => Data.Span;
    }

    public class CopyPayload : IPayload
    {
        private byte[] _data;
        public CopyPayload(IPayload payload)
        {
            _data = payload.Data.ToArray();
        }
        public ReadOnlyMemory<byte> Data => _data;


        public void Dispose()
        {
        }
    }

    public class ProtoPayload : IPayload
    {
        private readonly IMessage _proto;

        public ProtoPayload(IMessage proto)
        {
            _proto = proto;
        }

        public IMessage GetProto()
        {
            return _proto;
        }

        public ReadOnlyMemory<byte> Data => _proto.ToByteArray();

        public void Dispose()
        {
        }
    }

    public class ByteStringPayload : IPayload
    {
        private readonly ByteString _byteString;

        public ByteStringPayload(ByteString byteString)
        {
            _byteString = byteString;
        }

        public void Dispose()
        {
        }

        public ReadOnlyMemory<byte> Data => _byteString.ToByteArray();
    }

    public class EmptyPayload : IPayload
    {
        public void Dispose()
        {
        }

        public ReadOnlyMemory<byte> Data => new ReadOnlyMemory<byte>();
    }

    public class FramePayload : IPayload
    {
        private NetMQFrame _frame;
        public ReadOnlyMemory<byte> Data => new (_frame.Buffer,0,_frame.MessageSize);
        public NetMQFrame Frame => _frame;

        public FramePayload(NetMQFrame frame)
        {
            _frame = frame;
        }
        public void Dispose()
        {
        }
    }
    public class PooledBytePayload : IPayload
    {
        private PooledByteBuffer _pooledByteBuffer;

        public ReadOnlyMemory<byte> Data => _pooledByteBuffer.AsMemory();

        public PooledBytePayload(PooledByteBuffer ringBuffer)
        {
            _pooledByteBuffer = ringBuffer;
        }

        public void Dispose()
        {
            _pooledByteBuffer.Clear();
        }
    }
}
