namespace PlayHouse.Service.Shared;

internal class ServiceException : Exception
{
    public ServiceException(string message, Exception cause) : base(message, cause)
    {
    }

    public ServiceException(string message) : base(message)
    {
    }

    public class DuplicatedMessageIndex(string message) : ServiceException(message);

    public class NotRegisterMethod(string message) : ServiceException(message);

    public class NotRegisterInstance(string message) : ServiceException(message);

    public class NotExistApiHeaderInfoException() : ServiceException("target request header is not exist");
}