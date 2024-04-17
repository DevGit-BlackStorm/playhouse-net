using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;

namespace PlayHouse.Production.Shared;

public class PlayhouseOption
{
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public ushort ServiceId { get; set; } 
    public IServiceProvider? ServiceProvider { get; set; }
    public int RequestTimeoutSec { get; set; } = 5;
    public bool ShowQps { get; set; }
    public int NodeId { get; set; } // 0~ 4096
    public int MaxBufferPoolSize = 1024 * 1024 * 100;

    public List<string> AddressServerEndpoints { get; set; } = new();
    public ushort AddressServerServiceId { get; set; }

    public Func<int, IPayload, ushort, IPacket>? PacketProducer { get; set; }

}
