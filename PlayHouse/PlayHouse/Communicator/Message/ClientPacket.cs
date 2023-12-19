using PlayHouse.Production;

namespace PlayHouse.Communicator.Message
{
    public class ClientPacket : IBasePacket
    {
        public Header Header { get; set; }
        public IPayload Payload;

        public ClientPacket(Header header,IPayload payload)
        {
            Header = header;
            Payload = payload;
        }
        public ReadOnlySpan<byte> Data => Payload.Data;

        public void Dispose()
        {
            Payload.Dispose();            

        }

        public IPayload MovePayload()
        {
            IPayload temp = Payload;
            Payload = new EmptyPayload();
            return temp;
        }

        public ushort GetMsgSeq()
        {
            return Header.MsgSeq;
        }

        public int GetMsgId()
        {
            return Header.MsgId;
        }
          

        public ushort ServiceId()
        {
            return Header.ServiceId;
        }

        public Packet ToPacket()
        {
            return new Packet(Header.MsgId, MovePayload());
        }
        
    }




}
