using CommonLib;
using NetMQ;
using PlayHouse.Communicator.Message;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session.Network
{
    internal sealed class PacketParser
    {
        
        private readonly LOG<PacketParser> _log = new ();
        private const int HeaderSize = 11;

        public PacketParser() { }

        public List<ClientPacket> Parse(RingBuffer buffer)
        {

            var packets = new List<ClientPacket>();


            while (buffer.Count >= HeaderSize)
            {
                try
                {
                    int bodySize = XBitConverter.ToHostOrder(buffer.PeekInt16(buffer.ReaderIndex));

                    // If the remaining buffer is smaller than the expected packet size, wait for more data
                    if (buffer.Count < bodySize + HeaderSize)
                    {
                        return packets;
                    }

                    buffer.Clear(2);

                    ushort serviceId = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    int msgId = XBitConverter.ToHostOrder(buffer.ReadInt32());
                    ushort msgSeq = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    byte stageIndex = buffer.ReadByte();

                    var body = new NetMQFrame(bodySize);

                    //var body = new PooledBuffer(bodySize);
                                        
                    buffer.Read(body.Buffer,0, bodySize);

                    var clientPacket = new ClientPacket(new Header(serviceId, msgId, msgSeq,0,stageIndex), new FramePayload(body));
                    packets.Add(clientPacket);

                }
                catch (Exception e)
                {
                    _log.Error(()=>$"{e}");
                }
            }

            return packets;
        }
    }
}