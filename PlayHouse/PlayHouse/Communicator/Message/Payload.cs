
using PlayHouse.Communicator.Message.buffer;

namespace PlayHouse.Communicator.Message
{
    using Google.Protobuf;
    using System.IO;
    using System;

    public interface IPayload : IDisposable
    {
        byte[] Data();
        void Output(Stream outputStream);
    }

    public class ProtoPayload : IPayload
    {
        private readonly IMessage _proto;

        public ProtoPayload(IMessage proto)
        {
            _proto = proto;
        }

        public byte[] Data()
        {
            return _proto.ToByteArray();
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

        public byte[] Data()
        {
            return _byteString.ToByteArray();
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
        public byte[] Data()
        {
            return Array.Empty<byte>();
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

        public byte[] Data()
        {
            return _buffer.Data;
        }

        public void Output(Stream outputStream)
        {
            outputStream.Write(_buffer.Data, 0, _buffer.Data.Length);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}
