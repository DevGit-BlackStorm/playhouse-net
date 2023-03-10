using Google.Protobuf;

namespace PlayHouse.Communicator.Message
{
    internal class ByteStringPayload : IPayload
    {
        public ByteString ByteString { get; set; }
        public ByteStringPayload(ByteString byteString)
        {
            ByteString = byteString;
        }
            
        public byte[] Data()
        {
            return ByteString.ToByteArray();
        }

        public void Dispose()
        {
        }
    }
}