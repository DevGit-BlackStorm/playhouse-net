using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    internal static class PacketProducer
    {

        public static void Init(Func<int , IPayload,bool, IPacket> CreateFunc)//int msgId, payload,is request packet return IPacket
        {
            _createFunc = CreateFunc;  
        }

        private  static Func<int,IPayload,bool,IPacket>? _createFunc { get; set; } //msgId,

        public static IPacket CreatePacket(int msgId,IPayload payload,bool isReqeust)
        {
            return _createFunc!.Invoke(msgId, payload,isReqeust);
        }
    }
}
