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
        public ReadOnlyMemory<byte> Data => Payload.Data;
        public ReadOnlySpan<byte> Span => Payload.DataSpan;

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

        public ushort MsgSeq => Header.MsgSeq;
        public string MsgId => Header.MsgId;
        public ushort ServiceId => Header.ServiceId;  

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
