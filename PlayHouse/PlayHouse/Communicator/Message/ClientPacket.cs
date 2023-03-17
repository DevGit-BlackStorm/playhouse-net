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

        public static ClientPacket Of(HeaderMsg headerMsg,IPayload payload)
        {
            return new ClientPacket(Header.Of(headerMsg), payload);
        }

        public Header Header { get; set; }
        public IPayload Payload;

        public ClientPacket(Header header,IPayload payload)
        {
            Header = header;
            Payload = payload;
        }
        public byte[] Data()
        {
            return Payload.Data();
        }

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

        public int GetMsgSeq()
        {
            return Header.MsgSeq;
        }

        public string GetMsgName()
        {
            return Header.MsgName;
        }
          

        public string ServiceId()
        {
            return Header.ServiceId;
        }

        public  Packet ToPacket()
        {
            return new Packet(Header.MsgName, MovePayload());
        }
    }




}
