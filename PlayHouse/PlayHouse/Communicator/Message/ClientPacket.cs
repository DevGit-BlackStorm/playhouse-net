using Playhouse.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.Message
{
    public class ClientPacket : IBasePacket
    {

        //public static ClientPacket Of(HeaderMsg headerMsg,IPayload payload)
        //{
        //    return new ClientPacket(Header.Of(headerMsg), payload);
        //}

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

        public short GetMsgSeq()
        {
            return Header.MsgSeq;
        }

        public int GetMsgId()
        {
            return Header.MsgId;
        }
          

        public short ServiceId()
        {
            return Header.ServiceId;
        }

        public  Packet ToPacket()
        {
            return new Packet(Header.MsgId, MovePayload());
        }
        
    }




}
