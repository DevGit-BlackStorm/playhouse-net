using PlayHouse.Service;
namespace PlayHouse.Production.Shared;



internal class AsyncCore : IAsyncCore
{

    public AsyncCore() { }

    private readonly AsyncLocal<List<SendPacketInfo>?> _sendPackets = new();

    public void Init()
    {
        _sendPackets.Value = new();
    }


    public List<SendPacketInfo> GetSendPackets()
    {
        return _sendPackets.Value != null ? _sendPackets.Value : new();
    }

    public void Add(SendTarget target, ushort msgSeq, IPacket? packet)
    {
        if (_sendPackets.Value != null)
        {
            _sendPackets.Value.Add(new SendPacketInfo { Target = target, Packet = packet, MsgSeq = msgSeq });
        }
    }

    public void Add(SendTarget target, ushort msgSeq, ushort errorCode)
    {
        if (_sendPackets.Value != null)
        {
            _sendPackets.Value.Add(new SendPacketInfo { Target = target, Packet = null, ErrorCode = errorCode, MsgSeq = msgSeq });
        }
    }

    public void Clear()
    {
        _sendPackets.Value = null;
    }

}


public class PacketContext
{
    private IAsyncCore _core = new AsyncCore();
    internal static IAsyncCore AsyncCore
    {
        get { return Instance._core; }
        set { Instance._core = value; }
    }
    public static PacketContext Instance { get; private set; } = new();
    public static List<SendPacketInfo> SendPackets => AsyncCore.GetSendPackets();

}