
namespace PlayHouse.Communicator.Message
{
    using Google.Protobuf;
    using System.IO;
    using System;
    using CommonLib;

    public interface IPayload : IDisposable
    {
        (byte[],int) Data();
        void Output(Stream outputStream);
    }

    public class ProtoPayload : IPayload
    {
        private readonly IMessage _proto;

        public ProtoPayload(IMessage proto)
        {
            _proto = proto;
        }

        public (byte[],int) Data()
        {
            return (_proto.ToByteArray(),_proto.CalculateSize());
        }

        public void Output(Stream outputStream)
        {
            _proto.WriteTo(outputStream);
        }

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

        public (byte[],int) Data()
        {
            return (_byteString.ToByteArray(),_byteString.Length);
        }

        public void Output(Stream outputStream)
        {
            _byteString.WriteTo(outputStream);
        }

        public void Dispose()
        {
        }
    }

    public class EmptyPayload : IPayload
    {
        public (byte[],int) Data()
        {
            return (Array.Empty<byte>(),0);
        }

        public void Output(Stream outputStream)
        {
        }

        public void Dispose()
        {
        }
    }

    public class PooledBufferPayload : IPayload
    {
        private readonly PooledBuffer _buffer;

        public PooledBufferPayload(PooledBuffer buffer)
        {
            _buffer = buffer;
        }

        public (byte[], int) Data()
        {
            return (_buffer.Data,_buffer.Size);
        }

        public void Output(Stream outputStream)
        {
            outputStream.Write(_buffer.Data, 0, _buffer.Size);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}
