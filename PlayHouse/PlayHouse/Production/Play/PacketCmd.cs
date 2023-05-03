using PlayHouse.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Play
{
    public interface IPacketCmd<S, A> where A : IActor
    {
        public Task Execute(S stage, A actor, Packet packet);
    }
}
