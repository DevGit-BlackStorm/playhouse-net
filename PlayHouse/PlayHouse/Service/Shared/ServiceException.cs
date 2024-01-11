using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Shared;

internal class ServiceException : Exception
{
    public ServiceException(string message, Exception cause) : base(message, cause)
    {
    }

    public ServiceException(string message) : base(message)
    {
    }

    public class DuplicatedMessageIndex : ServiceException
    {
        public DuplicatedMessageIndex(string message) : base(message)
        {
        }
    }

    public class NotRegisterMethod : ServiceException
    {
        public NotRegisterMethod(string message) : base(message)
        {
        }
    }

    public class NotRegisterInstance : ServiceException
    {
        public NotRegisterInstance(string message) : base(message)
        {
        }
    }

    public class NotExistApiHeaderInfoException : ServiceException
    {
        public NotExistApiHeaderInfoException() : base("target request header is not exist")
        {
        }
    }
}

