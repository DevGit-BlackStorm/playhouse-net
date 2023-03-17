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
        private IPayload _payload;

        public ClientPacket(Header header,IPayload payload)
        {
            Header = header;
            _payload = payload;
        }
        public byte[] Data()
        {
            return _payload.Data();
        }

        public void Dispose()
        {
            _payload.Dispose();            

        }

        public IPayload MovePayload()
        {
            IPayload temp = _payload;
            _payload = XPayload.Empty();
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
