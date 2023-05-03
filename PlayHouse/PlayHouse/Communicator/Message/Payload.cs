
namespace PlayHouse.Communicator.Message
{
    using Google.Protobuf;
    using System.IO;
    using System;
    using CommonLib;
    using NetMQ;

    public interface IPayload : IDisposable
    {
        ReadOnlySpan<byte> Data { get; }
        //void Output(Stream outputStream);
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

        //public void Output(Stream outputStream)
        //{
        //    _proto.WriteTo(outputStream);
        //}

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

        
        //public void Output(Stream outputStream)
        //{
        //    _byteString.WriteTo(outputStream);
        //}

        public void Dispose()
        {
        }

        public ReadOnlySpan<byte> Data => _byteString.ToByteArray();
    }

    public class EmptyPayload : IPayload
    {

        //public void Output(Stream outputStream)
        //{
        //}

        public void Dispose()
        {
        }

        public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>();
        
    }

    //public class PooledBufferPayload : IPayload
    //{
    //    private readonly PooledBuffer _buffer;

    //    public PooledBufferPayload(PooledBuffer buffer)
    //    {
    //        _buffer = buffer;
    //    }

    //    public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>(_buffer.Data, 0, _buffer.Size);


    //    public void Dispose()
    //    {
    //        _buffer.Dispose();
    //    }
    //}

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
