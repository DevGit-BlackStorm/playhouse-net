
using CommonLib;
using Google.Protobuf;
using NetMQ;

namespace PlayHouse.Communicator.Message
{
    public interface IPayload : IDisposable
    {
        ReadOnlySpan<byte> Data { get; }
    }

    public class CopyPayload : IPayload
    {
        private byte[] _data;
        public CopyPayload(IPayload payload)
        {
            _data = payload.Data.ToArray();
        }
        public ReadOnlySpan<byte> Data => _data;

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

        public ReadOnlySpan<byte> Data => _proto.ToByteArray();

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

        public ReadOnlySpan<byte> Data => _byteString.ToByteArray();
    }

    public class EmptyPayload : IPayload
    {
        public void Dispose()
        {
        }

        public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>();
    }

    public class FramePayload : IPayload
    {
        private NetMQFrame _frame;
        public ReadOnlySpan<byte> Data => new (_frame.Buffer,0,_frame.MessageSize);
        public NetMQFrame Frame => _frame;

        public FramePayload(NetMQFrame frame)
        {
            _frame = frame;
        }
        public void Dispose()
        {
        }
    }
    public class RingBufferPayload : IPayload
    {
        private RingBuffer _ringBuffer;

        public ReadOnlySpan<byte> Data => new (_ringBuffer.Buffer(),0,_ringBuffer.Count);

        public RingBufferPayload(RingBuffer ringBuffer)
        {
            _ringBuffer = ringBuffer;
        }

        public void Dispose()
        {
            _ringBuffer.Clear();
        }
    }
}
