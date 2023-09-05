using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production
{
    public static class ExceptionContextStorage
    {
        private static readonly AsyncLocal<ushort> _errorCode = new AsyncLocal<ushort>();

        public static ushort ErrorCode
        {
            get =>  _errorCode.Value;
            set => _errorCode.Value = value;   
        }
    }
}
