
using Google.Protobuf;
using NetMQ;

namespace PlayHouse.Communicator.Message
{
    public interface IPayload : IDisposable
    {
        ReadOnlySpan<byte> Data { get; }
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
        public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>(_frame.Buffer,0,_frame.MessageSize);
        public NetMQFrame Frame => _frame;
        public FramePayload(NetMQFrame frame)
        {
            _frame = frame;
        }
        public void Dispose()
        {
        }
    }
}
