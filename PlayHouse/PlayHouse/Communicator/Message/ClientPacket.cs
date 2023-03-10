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
            _payload = Payload.Empty();
            return temp;
        }

        internal int GetMsgSeq()
        {
            throw new NotImplementedException();
        }

        internal object MsgName()
        {
            throw new NotImplementedException();
        }

        internal ReplyPacket ToReplyPacket()
        {
            throw new NotImplementedException();
        }
    }




}
