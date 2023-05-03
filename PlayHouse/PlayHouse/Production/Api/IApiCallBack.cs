using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Api
{
    public interface IApiCallBack
    {
        void OnDisconnect(long accountId);
    }
}
