using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    public static class ControlContext
    {
        public static ISender? BaseSender { get; set; }
        public static ISystemPanel? SystemPanel { get; set; }
    }

}
