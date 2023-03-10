using Google.Protobuf;

namespace PlayHouse.Communicator.Message
{
    internal class IMessagePayload : IPayload
    {
        public IMessage Message { get; private set; }
        public IMessagePayload(IMessage message) {
            Message = message;
        }
        public byte[] Data()
        {
            return Message.ToByteArray();
        }

        public void Dispose()
        {   
        }
    }
}