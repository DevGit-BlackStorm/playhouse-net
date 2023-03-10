using Google.Protobuf;
using PlayHouse.Communicator.Message.buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.Message
{
    public interface IPayload : IDisposable
    {
        byte[] Data();
    }

    public class Payload : IPayload
    {
        //private ByteString? message;
        private PooledBuffer _data;

        public Payload(PooledBuffer buffer) {
            _data = buffer;
        }

        public Payload(byte[] payload)
        {
            _data = new PooledBuffer(payload);
        }

        public static Payload Empty()
        {
            return new Payload(new PooledBuffer());
        }

        public byte[] Data()
        {
            return _data.Data;
        }

        public void Dispose()
        {
            _data?.Dispose();
        }
    }
}
