
namespace PlayHouse.Communicator.Message
{
    using Google.Protobuf;
    using System.IO;
    using System;
    using CommonLib;

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

    public class PooledBufferPayload : IPayload
    {
        private readonly PooledBuffer _buffer;

        public PooledBufferPayload(PooledBuffer buffer)
        {
            _buffer = buffer;
        }

        public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>(_buffer.Data, 0, _buffer.Size);

        //public (byte[], int) Data()
        //{
        //    return (_buffer.Data,_buffer.Size);
        //}

        //public void Output(Stream outputStream)
        //{
        //    outputStream.Write(_buffer.Data, 0, _buffer.Size);
        //}

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}
