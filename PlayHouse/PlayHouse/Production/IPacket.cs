using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production
{
    public interface IPacket
    {
        public int MsgId { get; }
//        public ReadOnlySpan<byte> Data { get; }
        public IPayload Payload { get; }

        public IPacket Copy();
        public T Parse<T>() ;

    }

}
