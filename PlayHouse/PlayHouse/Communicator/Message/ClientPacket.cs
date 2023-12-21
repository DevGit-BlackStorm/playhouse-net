using PlayHouse.Service;

namespace PlayHouse.Communicator.Message
{
    internal class ClientPacket : IBasePacket
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

        internal RoutePacket ToRoutePacket()
        {
            return RoutePacket.Of(Header.MsgId,MovePayload());
        }

        //public Packet ToPacket()
        //{
        //    return new Packet(Header.MsgId, MovePayload());
        //}

    }




}
