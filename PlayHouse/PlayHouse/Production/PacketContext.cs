using PlayHouse.Service;
namespace PlayHouse.Production;



internal class AsyncCore : IAsyncCore
{
    
    public AsyncCore() { }

    private readonly AsyncLocal<List<(SendTarget target, IPacket packet)>?> _sendPackets = new();

    public void Init()
    {
        _sendPackets.Value = new();
    }


    public List<(SendTarget target, IPacket packet)> GetSendPackets()
    {
        return _sendPackets.Value != null ? _sendPackets.Value : new();
    }

    public void Add(SendTarget target, IPacket packet)
    {
        if (_sendPackets.Value != null)
        {
            _sendPackets.Value.Add((target, packet));
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
    public static List<(SendTarget target, IPacket packet)> SendPackets => AsyncCore.GetSendPackets();

}