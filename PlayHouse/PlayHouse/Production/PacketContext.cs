using PlayHouse.Service;
namespace PlayHouse.Production;



internal class AsyncCore : IAsyncCore
{
    
    public AsyncCore() { }

    private readonly AsyncLocal<List<(SendTarget target, IPacket packet)>?> _sendPackets = new();
    private readonly AsyncLocal<bool> _isRequest = new();

    public void Init(bool isRequest)
    {
        _sendPackets.Value = new();
        _isRequest.Value = isRequest;
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
        _isRequest.Value = false;
    }

    public bool IsRequest()
    {
        return _isRequest.Value;
    }
    public void SetRequest()
    {
        _isRequest.Value = true;
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
    public static bool IsRequest => Instance._core.IsRequest();
    public static List<(SendTarget target, IPacket packet)> SendPackets => AsyncCore.GetSendPackets();

}