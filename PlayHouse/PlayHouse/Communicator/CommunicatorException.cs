namespace PlayHouse.Communicator;

public class CommunicatorException : Exception
{
    public CommunicatorException(string message, Exception cause) : base(message, cause)
    {
    }

    public CommunicatorException(string message) : base(message)
    {
    }

    public class NotExistServerInfo(string message) : CommunicatorException(message);
}