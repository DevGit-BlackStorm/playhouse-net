namespace PlayHouse.Production.Session
{
    public class SessionOption
    {
        public List<string> Urls { get; set; }
        public int SessionPort { get; set; }
        public bool UseWebSocket { get; set; }

        public int ClientIdleTimeoutMSec = 0; //  0인경우 idle확인 안함

        public SessionOption()
        {
            Urls = new List<string>();
            SessionPort = 0;
            UseWebSocket = false;
        }

    }
}
