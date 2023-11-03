namespace PlayHouse.Communicator
{
    public class CommunicatorException : Exception
    {
        public CommunicatorException(string message, Exception cause) : base(message, cause) { }
        public CommunicatorException(string message) : base(message) { }

        public class NotExistServerInfo : CommunicatorException
        {
            public NotExistServerInfo(string message) : base(message) { }
        }
    }
}
