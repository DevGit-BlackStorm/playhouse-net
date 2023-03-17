
using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.Message.buffer;
using System.Net;

namespace PlayHouse.Service.Session
{
    public class PacketParser
    {

        public const int MAX_PACKET_SIZE = 65535;
        public const int HEADER_SIZE = 256;
        public const int LENGTH_FIELD_SIZE = 3;
        private readonly ILogger _log;

        public PacketParser(ILogger log)
        {
            _log = log;
        }

        public virtual List<ClientPacket> Parse(PooledBuffer buffer)
        {

            var packets = new List<ClientPacket>();

            while (buffer.Size >= LENGTH_FIELD_SIZE)
            {
                try
                {

                    int headerSize = buffer[0];

                    if (headerSize > HEADER_SIZE)
                    {
                        _log.Error($"Header size over : {headerSize}", typeof(PacketParser).Name) ;
                        throw new IndexOutOfRangeException("HeaderSizeOver");
                    }

                    int bodySize = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(new Span<byte>(buffer.Data, 1, 2)));

                    if (bodySize > MAX_PACKET_SIZE)
                    {
                        _log.Error($"Body size over : {bodySize}", typeof(PacketParser).Name);
                        throw new IndexOutOfRangeException("BodySizeOver");
                    }

                    // If the remaining buffer is smaller than the expected packet size, wait for more data
                    if (buffer.Size < bodySize + LENGTH_FIELD_SIZE)
                    {
                        break;
                    }

                    var header = HeaderMsg.Parser.ParseFrom(new Span<byte>(buffer.Data, LENGTH_FIELD_SIZE, headerSize));

                    var body = new PooledBuffer(bodySize);
                    body.Append(new Span<byte>(buffer.Data, LENGTH_FIELD_SIZE + headerSize, bodySize));


                    var clientPacket = new ClientPacket(Header.Of(header), new XPayload(body));
                    packets.Add(clientPacket);

                    buffer.Remove(0, LENGTH_FIELD_SIZE + headerSize + bodySize);

                }
                catch (Exception e)
                {
                    _log.Error("Exception while parsing packet",typeof(PacketParser).Name,e);
                }
            }

            return packets;
        }
    }
}