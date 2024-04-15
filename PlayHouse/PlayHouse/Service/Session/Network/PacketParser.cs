using CommonLib;
using NetMQ;
using PlayHouse.Communicator.Message;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session.Network;

/*
 *  2byte  header size
 *  3byte  body size
 *  2byte  serviceId
 *  1byte  msgId size
 *  n byte msgId string
 *  2byte  msgSeq
 *  8byte  stageId
 *  From Header Size = 2+3+2+1+2+8+2+N = 18 + n
 * */

internal sealed class PacketParser
{
    public const int MAX_PACKET_SIZE = 16777215;
    public const int MIN_SIZE = 18;

    private readonly LOG<PacketParser> _log = new ();

    public PacketParser() { }

    public List<ClientPacket> Parse(RingBuffer buffer)
    {

        var packets = new List<ClientPacket>();


        while (buffer.Count >= MIN_SIZE)
        {
            try
            {
                int headerSize = buffer.PeekInt16(buffer.ReaderIndex);
                int bodySize = buffer.PeekInt24(buffer.MoveIndex(buffer.ReaderIndex,2));

                if (bodySize > MAX_PACKET_SIZE)
                {
                    _log.Error(() => $"Body size over : {bodySize}");
                    throw new IndexOutOfRangeException("BodySizeOver");
                }

                // If the remaining buffer is smaller than the expected packet size, wait for more data
                if (buffer.Count < bodySize + headerSize)
                {
                    break;
                }

                buffer.Clear(5);

                ushort serviceId = buffer.ReadInt16();
                byte sizeOfMsgName = buffer.ReadByte();
                string msgId = buffer.ReadString(sizeOfMsgName);

                ushort msgSeq = buffer.ReadInt16();
                long stageId = buffer.ReadInt64();
                var body = new NetMQFrame(bodySize);

                //var body = new PooledBuffer(bodySize);
                                    
                buffer.Read(body.Buffer,0, bodySize);

                var clientPacket = new ClientPacket(new Header(serviceId, msgId, msgSeq,0, stageId), new FramePayload(body));
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