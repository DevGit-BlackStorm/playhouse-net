namespace PlayHouse.Production.Session;

public class SessionOption
{
    public int ClientIdleTimeoutMSec = 30000; //5000  0인경우 idle확인 안함

    public List<string> Urls { get; set; } = new();
    public int SessionPort { get; set; } = 0;
    public bool UseWebSocket { get; set; } = false;

    public Func<ISessionUser>? SessionUserFactory = null;
}