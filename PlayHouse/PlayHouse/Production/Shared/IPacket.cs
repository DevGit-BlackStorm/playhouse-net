using PlayHouse.Communicator.Message;

namespace PlayHouse.Production.Shared
{
    public interface IPacket : IDisposable
    {
        public string MsgId { get; }
        public IPayload Payload { get; }
        //public IPacket Copy();
        //public T Parse<T>();

    }

}
