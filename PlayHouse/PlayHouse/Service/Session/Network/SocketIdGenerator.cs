using PlayHouse.Service.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Session.Network;

public static class SocketIdGenerator
{
    public static readonly UniqueIdGenerator IdGenerator = new UniqueIdGenerator(0);
}