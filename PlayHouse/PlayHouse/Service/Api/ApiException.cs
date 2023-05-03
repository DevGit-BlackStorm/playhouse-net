using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Api
{
    public class ApiException : Exception
    {
        public ApiException(string message, Exception cause) : base(message, cause)
        {
        }

        public ApiException(string message) : base(message)
        {
        }

        public class DuplicatedMessageIndex : ApiException
        {
            public DuplicatedMessageIndex(string message) : base(message)
            {
            }
        }

        public class NotRegisterApiMethod : ApiException
        {
            public NotRegisterApiMethod(string message) : base(message)
            {
            }
        }

        public class NotRegisterApiInstance : ApiException
        {
            public NotRegisterApiInstance(string className) : base($"{className}: ApiInstance is not registered")
            {
            }
        }

        public class NotExistApiHeaderInfoException : ApiException
        {
            public NotExistApiHeaderInfoException() : base("target request header is not exist")
            {
            }
        }
    }
}
